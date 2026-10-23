using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Cache;
using Headwind.Mooha.Model;
using Headwind.Mooha.Source;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Covers what the cache decorator admits, which is the one decision it makes: a hit must not
    /// reach the wrapped source, and a result the cache cannot account for must not take an entry.
    /// </summary>
    public class CachedResourceSourceTests
    {
        private readonly TrackedObjects _objects = new();

        private IResourceSource<FakeRequest<Texture2D>, Texture2D> _source;
        private CachedResourceSource<FakeRequest<Texture2D>, Texture2D> _cached;

        [SetUp]
        public void WrapASubstitutedSource()
        {
            _source = Substitute.For<IResourceSource<FakeRequest<Texture2D>, Texture2D>>();
            _cached = new CachedResourceSource<FakeRequest<Texture2D>, Texture2D>(
                _source,
                new ResourceCacheStore(new ResourceCacheBudget(1024 * 1024))
            );
        }

        [TearDown]
        public void DestroyTrackedObjects() => _objects.DestroyAll();

        [Test]
        public void ASecondLoadOfOneRequestNeverReachesTheSource()
        {
            Loads(_objects.NewTexture(), Substitute.For<IDisposable>(), 16);

            var first = Load("sword");
            var second = Load("sword");

            ReceivedLoads(1);
            Assert.That(second.Value, Is.SameAs(first.Value));
        }

        /// <summary>
        /// No cost means the bytes are charged somewhere else — a sprite cloned out of a cached
        /// atlas — so an entry here would both double count them and stack entries in front of one
        /// shared resource.
        /// </summary>
        [Test]
        public void AResultReportingNoCostIsPassedThroughUncached()
        {
            Loads(_objects.NewTexture(), Substitute.For<IDisposable>(), 0);

            Load("sword");
            Load("sword");

            ReceivedLoads(2);
        }

        [Test]
        public void AFailedLoadIsNotCached()
        {
            Loads(null, null, 16);

            Load("sword");
            Load("sword");

            ReceivedLoads(2);
        }

        private void Loads(Texture2D value, IDisposable release, long cost) =>
            _source
                .LoadAsync(Arg.Any<FakeRequest<Texture2D>>(), Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(new ResourceHandle<Texture2D>(value, release, cost)));

        private ResourceHandle<Texture2D> Load(string key) =>
            _cached
                .LoadAsync(new FakeRequest<Texture2D>(key), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

        private void ReceivedLoads(int count) =>
            _source
                .Received(count)
                .LoadAsync(Arg.Any<FakeRequest<Texture2D>>(), Arg.Any<CancellationToken>());
    }
}
