using System;
using System.Collections.Generic;
using UnityEngine;

namespace Headwind.Mooha.Size
{
    /// <summary>
    /// Asset-backed <see cref="IResourceSizeTable"/> that an editor bake writes and every runtime
    /// environment reads.
    /// <para>Entries store the resource type as a name since a Type cannot be serialized, and are
    /// resolved into a lookup once on first query.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Mooha/Resource Size Table", fileName = "ResourceSizeTable")]
    public sealed class ResourceSizeTable : ScriptableObject, IResourceSizeTable
    {
        [SerializeField]
        private List<SerializedEntry> _entries = new();

        private Dictionary<ResourceSizeKey, long> _lookup;

        /// <summary>
        /// Addresses the table currently holds, so a bake can report the ones that vanished.
        /// </summary>
        public IEnumerable<string> Addresses
        {
            get
            {
                foreach (var entry in _entries)
                    yield return entry.Address;
            }
        }

        public bool TryGetBytes(Type resourceType, string address, out long bytes)
        {
            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(new ResourceSizeKey(resourceType, address), out bytes);
        }

        /// <summary>
        /// The editor bake owns this table, so it writes the whole population at once and never
        /// merges into what was there before.
        /// </summary>
        public void Bake(IEnumerable<KeyValuePair<ResourceSizeKey, long>> sizes)
        {
            _entries.Clear();
            foreach (var size in sizes)
            {
                _entries.Add(
                    new SerializedEntry(
                        SerializedTypeName(size.Key.ResourceType),
                        size.Key.Address,
                        size.Value
                    )
                );
            }
            _lookup = null;
        }

        private Dictionary<ResourceSizeKey, long> BuildLookup()
        {
            var lookup = new Dictionary<ResourceSizeKey, long>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (TryResolveResourceType(entry, out var resourceType))
                    lookup[new ResourceSizeKey(resourceType, entry.Address)] = entry.Bytes;
            }
            return lookup;
        }

        /// <summary>
        /// Fails for an entry whose stored type name no longer resolves, which drops it from the
        /// lookup at the cost of a cache miss instead of a wrong size.
        /// </summary>
        private static bool TryResolveResourceType(SerializedEntry entry, out Type resourceType)
        {
            resourceType = Type.GetType(entry.TypeName);
            if (resourceType != null)
                return true;

            Debug.LogWarning(
                $"[Mooha] Size table entry '{entry.Address}' names a type that no longer "
                    + $"resolves ('{entry.TypeName}'); it will not be cached."
            );
            return false;
        }

        /// <summary>
        /// Assembly qualified without version or culture, which Type.GetType still resolves. The
        /// full form would pin every entry to the assembly versions present at bake time.
        /// </summary>
        private static string SerializedTypeName(Type type) =>
            $"{type.FullName}, {type.Assembly.GetName().Name}";

        [Serializable]
        private struct SerializedEntry
        {
            public string TypeName;
            public string Address;
            public long Bytes;

            public SerializedEntry(string typeName, string address, long bytes)
            {
                TypeName = typeName;
                Address = address;
                Bytes = bytes;
            }
        }
    }
}
