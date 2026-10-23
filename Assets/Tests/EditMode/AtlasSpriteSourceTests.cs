using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Headwind.Mooha.Source;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.U2D;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Covers the two ways an atlas sprite load comes back empty, both of which have to give the
    /// atlas lease back — holding it would pin a page texture for a sprite nobody got.
    /// <para>The success path needs a packed atlas, which only exists as a project asset, so it is
    /// left to integration coverage.</para>
    /// </summary>
    public class AtlasSpriteSourceTests
    {
        private readonly TrackedObjects _objects = new();

        private IResourceSource<AddressableSpriteAtlasRequest, SpriteAtlas> _atlasSource;
        private AtlasSpriteSource _source;
        private IDisposable _atlasRelease;

        [SetUp]
        public void SubstituteTheAtlasSource()
        {
            _atlasSource = Substitute.For<
                IResourceSource<AddressableSpriteAtlasRequest, SpriteAtlas>
            >();
            _source = new AtlasSpriteSource(_atlasSource);
            _atlasRelease = Substitute.For<IDisposable>();
        }

        [TearDown]
        public void DestroyTrackedObjects() => _objects.DestroyAll();

        [Test]
        public void AMissingAtlasGivesTheAtlasLeaseBack()
        {
            AtlasLoads(null);

            var handle = Load();

            Assert.That(handle.Value, Is.Null);
            _atlasRelease.Received(1).Dispose();
        }

        /// <summary>
        /// An atlas that does not hold the named sprite answers GetSprite with null, which is also
        /// what keeps a sub-object load Addressables would log an exception over from happening.
        /// </summary>
        [Test]
        public void AnAtlasWithoutTheNamedSpriteGivesTheAtlasLeaseBack()
        {
            AtlasLoads(_objects.Track(new SpriteAtlas()));

            var handle = Load();

            Assert.That(handle.Value, Is.Null);
            _atlasRelease.Received(1).Dispose();
        }

        private void AtlasLoads(SpriteAtlas atlas) =>
            _atlasSource
                .LoadAsync(
                    Arg.Any<AddressableSpriteAtlasRequest>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(
                    UniTask.FromResult(new ResourceHandle<SpriteAtlas>(atlas, _atlasRelease, 16))
                );

        private ResourceHandle<Sprite> Load() =>
            _source
                .LoadAsync(
                    new AtlasSpriteRequest("icons/items", "sword"),
                    CancellationToken.None
                )
                .GetAwaiter()
                .GetResult();
    }
}
