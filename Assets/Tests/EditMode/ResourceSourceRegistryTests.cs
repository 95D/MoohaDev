using System;
using System.Threading;
using Headwind.Mooha.Request;
using Headwind.Mooha.Source;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Covers the dispatch the registry exists for: a provider knows only the resource type, so the
    /// request's runtime type is what picks the source.
    /// <para>Sprite has two request types in the library — one addressable sprite, one packed into
    /// an atlas — which is exactly the ambiguity the registry resolves.</para>
    /// </summary>
    public class ResourceSourceRegistryTests
    {
        private ResourceSourceRegistry<Sprite> _registry;
        private IResourceSource<AtlasSpriteRequest, Sprite> _atlasSprites;
        private IResourceSource<AddressableSpriteRequest, Sprite> _standaloneSprites;

        [SetUp]
        public void RegisterOneSourcePerRequestType()
        {
            _registry = new ResourceSourceRegistry<Sprite>();
            _atlasSprites = Substitute.For<IResourceSource<AtlasSpriteRequest, Sprite>>();
            _standaloneSprites = Substitute.For<
                IResourceSource<AddressableSpriteRequest, Sprite>
            >();
            _registry.Register(_atlasSprites);
            _registry.Register(_standaloneSprites);
        }

        [Test]
        public void TheRequestTypeDecidesWhichSourceServesTheLoad()
        {
            _registry.LoadAsync(new AtlasSpriteRequest("icons/items", "sword"), default);

            _atlasSprites
                .Received(1)
                .LoadAsync(Arg.Any<AtlasSpriteRequest>(), Arg.Any<CancellationToken>());
            _standaloneSprites
                .DidNotReceive()
                .LoadAsync(Arg.Any<AddressableSpriteRequest>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Last write wins, so a second initialization — play mode start, domain reload — replaces
        /// the source instead of leaving the previous one and whatever state it had accumulated.
        /// </summary>
        [Test]
        public void RegisteringARequestTypeAgainReplacesItsSource()
        {
            var replacement = Substitute.For<IResourceSource<AtlasSpriteRequest, Sprite>>();
            _registry.Register(replacement);

            _registry.LoadAsync(new AtlasSpriteRequest("icons/items", "sword"), default);

            replacement
                .Received(1)
                .LoadAsync(Arg.Any<AtlasSpriteRequest>(), Arg.Any<CancellationToken>());
            _atlasSprites
                .DidNotReceive()
                .LoadAsync(Arg.Any<AtlasSpriteRequest>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// An unserved request is a wiring fault rather than a missing resource: resolving it to
        /// nothing would show the default and hide the registration that was never made.
        /// </summary>
        [Test]
        public void ARequestWithNoRegisteredSourceThrows()
        {
            var registry = new ResourceSourceRegistry<Texture2D>();

            Assert.That(registry.IsEmpty, Is.True);
            Assert.Throws<InvalidOperationException>(
                () => registry.LoadAsync(new FakeRequest<Texture2D>("sword"), default)
            );
        }
    }
}
