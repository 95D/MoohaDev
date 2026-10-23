using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Headwind.Mooha.Model;
using Headwind.Mooha.Result;
using Headwind.Mooha.Source;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Covers the state sequence a load emits, which is the whole of what a bound widget sees.
    /// <para>Providers are built through MoohaLoader because that is the only way to get one, so
    /// each test registers its own source and defaults; registration is per request type and last
    /// write wins, which is what keeps the tests independent of each other.</para>
    /// </summary>
    public class AsyncResourceProviderTests
    {
        private readonly TrackedObjects _objects = new();

        private IResourceSource<FakeRequest<Texture2D>, Texture2D> _source;
        private Texture2D _placeholder;
        private Texture2D _fallback;
        private Texture2D _resource;

        [SetUp]
        public void RegisterSourceAndDefaults()
        {
            _source = Substitute.For<IResourceSource<FakeRequest<Texture2D>, Texture2D>>();
            _placeholder = _objects.NewTexture();
            _fallback = _objects.NewTexture();
            _resource = _objects.NewTexture();
            MoohaLoader.RegisterSource(_source);
            MoohaLoader.SetDefaults(_placeholder, _fallback);
        }

        [TearDown]
        public void DestroyTrackedObjects() => _objects.DestroyAll();

        /// <summary>
        /// Rebinding something already resident must not flash the placeholder for a frame, so the
        /// placeholder is skipped when the load never suspends.
        /// </summary>
        [Test]
        public async Task ALoadThatNeverSuspendsSkipsThePlaceholder()
        {
            Resolves(UniTask.FromResult(new ResourceHandle<Texture2D>(_resource, null)));

            var results = await Collect();

            Assert.That(States(results), Is.EqualTo(new[] { ResourceLoadState.Resource }));
            Assert.That(results[0].Value, Is.SameAs(_resource));
        }

        [Test]
        public async Task ALoadThatSuspendsEmitsThePlaceholderFirst()
        {
            var loading = new UniTaskCompletionSource<ResourceHandle<Texture2D>>();
            Resolves(loading.Task);

            var collecting = Collect();
            loading.TrySetResult(new ResourceHandle<Texture2D>(_resource, null));
            var results = await collecting;

            Assert.That(
                States(results),
                Is.EqualTo(new[] { ResourceLoadState.Placeholder, ResourceLoadState.Resource })
            );
            Assert.That(results[0].Value, Is.SameAs(_placeholder));
        }

        [Test]
        public async Task AFailedLoadEmitsTheRegisteredFailedRef()
        {
            var failed = _objects.NewTexture();
            MoohaLoader.SetDefaults(
                _placeholder,
                _fallback,
                new ResourceRef<Texture2D>.ByValue(failed)
            );
            Resolves(UniTask.FromResult<ResourceHandle<Texture2D>>(default));

            var results = await Collect();

            Assert.That(States(results), Is.EqualTo(new[] { ResourceLoadState.Default }));
            Assert.That(results[0].Value, Is.SameAs(failed));
        }

        [Test]
        public async Task AFailedLoadWithNoFailedRefEmitsTheDefault()
        {
            Resolves(UniTask.FromResult<ResourceHandle<Texture2D>>(default));

            var results = await Collect();

            Assert.That(States(results), Is.EqualTo(new[] { ResourceLoadState.Default }));
            Assert.That(results[0].Value, Is.SameAs(_fallback));
        }

        /// <summary>
        /// A source that throws is a wiring or decoding fault, not a missing resource, so it is
        /// surfaced rather than quietly becoming a default.
        /// </summary>
        [Test]
        public async Task ASourceThatThrowsIsLoggedAndStillEmitsTheDefault()
        {
            Resolves(
                UniTask.FromException<ResourceHandle<Texture2D>>(
                    new InvalidOperationException("source fault")
                )
            );
            LogAssert.Expect(LogType.Exception, new Regex("source fault"));

            var results = await Collect();

            Assert.That(States(results), Is.EqualTo(new[] { ResourceLoadState.Default }));
            Assert.That(results[0].Value, Is.SameAs(_fallback));
        }

        /// <summary>
        /// The consumer that cancelled is no longer bound to anything the stream would feed, so it
        /// ends without a closing emission rather than falling back to the default.
        /// </summary>
        [Test]
        public async Task ACancelledLoadEmitsNothing()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Resolves(UniTask.FromCanceled<ResourceHandle<Texture2D>>(cancellation.Token));

            var results = await Collect();

            Assert.That(results, Is.Empty);
        }

        private void Resolves(UniTask<ResourceHandle<Texture2D>> resolving) =>
            _source
                .LoadAsync(Arg.Any<FakeRequest<Texture2D>>(), Arg.Any<CancellationToken>())
                .Returns(resolving);

        private static async UniTask<List<ResourceLoadResult<Texture2D>>> Collect()
        {
            var results = new List<ResourceLoadResult<Texture2D>>();
            await MoohaLoader
                .CreateProvider<Texture2D>()
                .Load(new FakeRequest<Texture2D>("sword"))
                .ForEachAsync(result => results.Add(result));
            return results;
        }

        private static ResourceLoadState[] States(
            IEnumerable<ResourceLoadResult<Texture2D>> results
        ) => results.Select(result => result.State).ToArray();
    }
}
