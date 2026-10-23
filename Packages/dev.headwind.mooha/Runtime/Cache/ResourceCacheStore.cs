using System;
using System.Collections.Generic;
using Headwind.Mooha.Model;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Cache
{
    /// <summary>
    /// Keeps released resources alive for a while so a re-request can skip the load.
    /// <para>Disposing the last lease on an entry does not release the resource; it demotes the
    /// entry into the LRU to wait for a re-request. Only demoted entries are charged against the
    /// budget, because an entry still in use is already bounded by whatever is on screen.</para>
    /// <para>Eviction is by recency alone — cost decides when the loop stops, never which entry
    /// goes. Evicting the largest first would discard whatever is most expensive to load again,
    /// and a resource big enough to matter is usually the one being looked at, which recency
    /// already keeps.</para>
    /// <para>One store is shared by every resource type. Splitting it per type would split the
    /// budget too, stranding free space in one half while the other evicts.</para>
    /// </summary>
    public sealed class ResourceCacheStore
    {
        private readonly ResourceCacheBudget _budget;
        private readonly Dictionary<ResourceCacheKey, Entry> _entries = new();

        /// <summary>
        /// Ordered eldest-first; holds only the entries whose lease count has fallen to zero.
        /// </summary>
        private readonly LinkedList<Entry> _idle = new();

        private long _idleBytes;

        public ResourceCacheStore(ResourceCacheBudget budget)
        {
            _budget = budget;
        }

        public bool TryAcquire<T>(ResourceCacheKey key, out ResourceHandle<T> handle)
            where T : Object
        {
            if (!_entries.TryGetValue(key, out var entry) || entry.Value is not T)
            {
                handle = default;
                return false;
            }

            handle = AcquireHandle<T>(entry);
            return true;
        }

        /// <summary>
        /// Takes ownership of a freshly loaded resource and hands back a leased handle for it.
        /// <para>A concurrent load of the same key can arrive after another has already been
        /// admitted. The late arrival is released rather than kept, so the two callers share one
        /// resource instead of one of them leaking.</para>
        /// </summary>
        public ResourceHandle<T> Admit<T>(ResourceCacheKey key, ResourceHandle<T> loaded)
            where T : Object
        {
            if (_entries.TryGetValue(key, out var resident))
            {
                loaded.Handle?.Dispose();
                return AcquireHandle<T>(resident);
            }

            var entry = new Entry
            {
                Key = key,
                Value = loaded.Value,
                Release = loaded.Handle,
                Cost = loaded.Cost,
            };
            _entries.Add(key, entry);
            return AcquireHandle<T>(entry);
        }

        private ResourceHandle<T> AcquireHandle<T>(Entry entry)
            where T : Object
        {
            AddLease(entry);
            return new ResourceHandle<T>((T)entry.Value, new Lease(this, entry), entry.Cost);
        }

        private void AddLease(Entry entry)
        {
            RemoveFromIdle(entry);
            entry.LeaseCount++;
        }

        private void ReleaseLease(Entry entry)
        {
            entry.LeaseCount--;
            if (entry.LeaseCount > 0)
                return;

            if (FitsBudgetAlone(entry))
                Demote(entry);
            else
                Evict(entry);
        }

        /// <summary>
        /// An entry that alone exceeds the budget would evict the whole cache to fit, so it is
        /// dropped instead of demoted.
        /// </summary>
        private bool FitsBudgetAlone(Entry entry) => entry.Cost <= _budget.MaxBytes;

        private void Demote(Entry entry)
        {
            entry.IdleNode = _idle.AddLast(entry);
            _idleBytes += entry.Cost;
            TrimToBudget();
        }

        private void TrimToBudget()
        {
            while (_idleBytes > _budget.MaxBytes && _idle.First != null)
                Evict(_idle.First.Value);
        }

        private void Evict(Entry entry)
        {
            RemoveFromIdle(entry);
            _entries.Remove(entry.Key);
            ReleaseResource(entry);
        }

        private void RemoveFromIdle(Entry entry)
        {
            if (entry.IdleNode == null)
                return;

            _idle.Remove(entry.IdleNode);
            entry.IdleNode = null;
            _idleBytes -= entry.Cost;
        }

        private static void ReleaseResource(Entry entry)
        {
            entry.Release?.Dispose();
            entry.Release = null;
            entry.Value = null;
        }

        private sealed class Entry
        {
            public ResourceCacheKey Key;
            public Object Value;
            public IDisposable Release;
            public long Cost;
            public int LeaseCount;
            public LinkedListNode<Entry> IdleNode;
        }

        /// <summary>One caller's claim on an entry, given up by disposing it.</summary>
        private sealed class Lease : IDisposable
        {
            private ResourceCacheStore _store;
            private Entry _entry;

            public Lease(ResourceCacheStore store, Entry entry)
            {
                _store = store;
                _entry = entry;
            }

            public void Dispose()
            {
                if (_entry == null)
                    return;
                var store = _store;
                var entry = _entry;
                _store = null;
                _entry = null;
                store.ReleaseLease(entry);
            }
        }
    }
}
