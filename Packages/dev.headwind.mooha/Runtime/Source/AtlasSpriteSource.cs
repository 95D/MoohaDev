using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Loads a sprite out of a SpriteAtlas, acquiring the atlas from its own source first.
    /// <para>A null GetSprite result doubles as the sub-sprite existence check, so a sub-object
    /// load that would make Addressables' ResourceManager log an exception is never attempted.
    /// </para>
    /// <para>A clone off an atlas reports no cost of its own. The bytes belong to the atlas page it
    /// points into, which the atlas layer already accounts for, and the clone is a per-caller shell
    /// that is never shared.</para>
    /// </summary>
    public class AtlasSpriteSource : IResourceSource<AtlasSpriteRequest, Sprite>
    {
        private readonly IResourceSource<AddressableSpriteAtlasRequest, SpriteAtlas> _atlasSource;

        public AtlasSpriteSource(
            IResourceSource<AddressableSpriteAtlasRequest, SpriteAtlas> atlasSource
        )
        {
            _atlasSource = atlasSource;
        }

        public async UniTask<ResourceHandle<Sprite>> LoadAsync(
            AtlasSpriteRequest request,
            CancellationToken cancellationToken
        )
        {
            var atlas = await _atlasSource.LoadAsync(
                new AddressableSpriteAtlasRequest(request.AtlasAddress),
                cancellationToken
            );
            if (atlas.Value == null)
            {
                atlas.Handle?.Dispose();
                return default;
            }

            var sprite = atlas.Value.GetSprite(request.SpriteName);
            if (sprite == null)
            {
                atlas.Handle?.Dispose();
                return default;
            }
            return new ResourceHandle<Sprite>(
                sprite,
                new SpriteAtlasCloneRelease(atlas.Handle, sprite)
            );
        }

        /// <summary>
        /// Owns the GetSprite clone alongside the atlas it came from: the clone is destroyed and
        /// the atlas given up together, so the shared page texture stays resident for the clone's
        /// lifetime.
        /// </summary>
        private sealed class SpriteAtlasCloneRelease : IDisposable
        {
            private IDisposable _atlas;
            private Sprite _sprite;

            public SpriteAtlasCloneRelease(IDisposable atlas, Sprite sprite)
            {
                _atlas = atlas;
                _sprite = sprite;
            }

            public void Dispose()
            {
                DestroyClone();
                _atlas?.Dispose();
                _atlas = null;
            }

            /// <summary>
            /// The clone is not a persistent asset, so destroying it outright is safe. Edit mode
            /// forbids the deferred Destroy, hence the branch on play state.
            /// </summary>
            private void DestroyClone()
            {
                if (_sprite == null)
                    return;

                if (Application.isPlaying)
                    Object.Destroy(_sprite);
                else
                    Object.DestroyImmediate(_sprite);
                _sprite = null;
            }
        }
    }
}
