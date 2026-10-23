using System;
using Headwind.Mooha.Cache;
using Headwind.Mooha.Model;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Covers the store's two accounting rules — the last lease demotes an entry instead of
    /// releasing it, and only demoted entries are charged to the budget — and the eviction order
    /// that follows from them.
    /// <para>An entry's release is a substituted IDisposable, so whether the store dropped a
    /// resource is observable without loading anything.</para>
    /// </summary>
    public class ResourceCacheStoreTests
    {
        private const long Megabyte = 1024 * 1024;

        private readonly TrackedObjects _objects = new();

        [TearDown]
        public void DestroyTrackedObjects() => _objects.DestroyAll();

        [Test]
        public void TheLastLeaseDemotesTheEntryInsteadOfReleasingIt()
        {
            var store = new ResourceCacheStore(new ResourceCacheBudget(Megabyte));
            var release = Substitute.For<IDisposable>();
            var leased = store.Admit(Key("sword"), Loaded(release, 16));

            leased.Handle.Dispose();

            release.DidNotReceive().Dispose();
            Assert.That(store.TryAcquire<Texture2D>(Key("sword"), out var reacquired), Is.True);
            Assert.That(reacquired.Value, Is.SameAs(leased.Value));
        }

        /// <summary>
        /// The budget counts demoted bytes, so returning an entry to active use has to take its
        /// cost back off. Leaving it on would inflate the total and evict entries that fit.
        /// </summary>
        [Test]
        public void ReacquiringADemotedEntryTakesItsCostBackOffTheBudget()
        {
            var store = new ResourceCacheStore(new ResourceCacheBudget(10));
            var release = Substitute.For<IDisposable>();
            store.Admit(Key("sword"), Loaded(release, 6)).Handle.Dispose();

            store.TryAcquire<Texture2D>(Key("sword"), out var reacquired);
            reacquired.Handle.Dispose();

            // Double counting would put twelve demoted bytes under a ten byte budget and evict the
            // only entry the cache holds.
            release.DidNotReceive().Dispose();
            Assert.That(store.TryAcquire<Texture2D>(Key("sword"), out _), Is.True);
        }

        /// <summary>
        /// The trace from DevNotes §2.1: cost decides when the loop stops, never which entry goes.
        /// </summary>
        [Test]
        public void EvictionFollowsRecencyRatherThanCost()
        {
            var store = new ResourceCacheStore(new ResourceCacheBudget(16 * Megabyte));
            var eldest = Demote(store, "eldest", Megabyte / 2);
            var second = Demote(store, "second", Megabyte / 2);
            var third = Demote(store, "third", Megabyte / 2);
            var largest = Demote(store, "largest", 6 * Megabyte);
            var newest = Demote(store, "newest", Megabyte / 2);

            // Eight megabytes are resident; ten more do not fit under a sixteen megabyte budget.
            var arrival = Demote(store, "arrival", 10 * Megabyte);

            eldest.Received(1).Dispose();
            second.Received(1).Dispose();
            third.Received(1).Dispose();
            largest.Received(1).Dispose();
            newest.DidNotReceive().Dispose();
            arrival.DidNotReceive().Dispose();
        }

        [Test]
        public void AnEntryThatAloneExceedsTheBudgetIsReleasedRatherThanDemoted()
        {
            var store = new ResourceCacheStore(new ResourceCacheBudget(10));

            var release = Demote(store, "oversized", 11);

            release.Received(1).Dispose();
            Assert.That(store.TryAcquire<Texture2D>(Key("oversized"), out _), Is.False);
        }

        [Test]
        public void AnEntryStillInUseIsNotChargedToTheBudget()
        {
            var store = new ResourceCacheStore(new ResourceCacheBudget(10));
            var activeRelease = Substitute.For<IDisposable>();
            store.Admit(Key("onscreen"), Loaded(activeRelease, 10));

            var idleRelease = Demote(store, "offscreen", 10);

            idleRelease.DidNotReceive().Dispose();
            activeRelease.DidNotReceive().Dispose();
        }

        /// <summary>
        /// A second load of one key can land after the first has been admitted. The late handle is
        /// the one released, and both callers hold a lease on the one resident resource.
        /// </summary>
        [Test]
        public void AdmittingAKeyTwiceReleasesTheLateHandleAndSharesTheResident()
        {
            var store = new ResourceCacheStore(new ResourceCacheBudget(Megabyte));
            var residentRelease = Substitute.For<IDisposable>();
            var lateRelease = Substitute.For<IDisposable>();
            var resident = store.Admit(Key("sword"), Loaded(residentRelease, 16));

            var late = store.Admit(Key("sword"), Loaded(lateRelease, 16));

            lateRelease.Received(1).Dispose();
            Assert.That(late.Value, Is.SameAs(resident.Value));

            resident.Handle.Dispose();
            Assert.That(
                store.TryAcquire<Texture2D>(Key("sword"), out _),
                Is.True,
                "the second lease still holds the entry"
            );
        }

        /// <summary>
        /// Disposing one lease twice would drive the count below what is actually held, so an entry
        /// in use would be evicted out from under its holder.
        /// </summary>
        [Test]
        public void DisposingALeaseTwiceDoesNotGiveUpTheEntry()
        {
            // A budget of zero evicts on release instead of demoting, which is what makes the
            // premature release observable.
            var store = new ResourceCacheStore(new ResourceCacheBudget(0));
            var release = Substitute.For<IDisposable>();
            var first = store.Admit(Key("sword"), Loaded(release, 16));
            store.TryAcquire<Texture2D>(Key("sword"), out _);

            first.Handle.Dispose();
            first.Handle.Dispose();

            release.DidNotReceive().Dispose();
            Assert.That(store.TryAcquire<Texture2D>(Key("sword"), out _), Is.True);
        }

        [Test]
        public void TryAcquireRejectsAnEntryHoldingAnotherResourceType()
        {
            var store = new ResourceCacheStore(new ResourceCacheBudget(Megabyte));
            store.Admit(Key("sword"), Loaded(Substitute.For<IDisposable>(), 16));

            Assert.That(store.TryAcquire<Material>(Key("sword"), out var handle), Is.False);
            Assert.That(handle.Value, Is.Null);
        }

        /// <summary>Admits an entry and gives up its lease, returning the release to assert on.
        /// </summary>
        private IDisposable Demote(ResourceCacheStore store, string key, long cost)
        {
            var release = Substitute.For<IDisposable>();
            store.Admit(Key(key), Loaded(release, cost)).Handle.Dispose();
            return release;
        }

        private ResourceHandle<Texture2D> Loaded(IDisposable release, long cost) =>
            new(_objects.NewTexture(), release, cost);

        private static ResourceCacheKey Key(string key) =>
            new(typeof(FakeRequest<Texture2D>), key);
    }
}
