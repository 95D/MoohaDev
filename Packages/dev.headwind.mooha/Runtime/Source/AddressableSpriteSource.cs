using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Headwind.Mooha.Size;
using UnityEngine;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Loads standalone Sprite addressables — a sprite asset of its own, not one packed into an
    /// atlas, which <see cref="AtlasSpriteSource"/> serves instead.
    /// </summary>
    public class AddressableSpriteSource : IResourceSource<AddressableSpriteRequest, Sprite>
    {
        private readonly AddressableKeyExistence _keyExistence = new();

        public async UniTask<ResourceHandle<Sprite>> LoadAsync(
            AddressableSpriteRequest request,
            CancellationToken cancellationToken
        )
        {
            if (!await _keyExistence.ExistsAsync(request.Address, cancellationToken))
                return default;

            var handle = await AddressableHandleLoader.LoadAsync<Sprite>(
                request.Address,
                cancellationToken
            );
            if (!handle.IsValid())
                return default;

            var sprite = handle.Result;
            return new ResourceHandle<Sprite>(
                sprite,
                new AddressableHandleRelease<Sprite>(handle),
                TextureByteSize.Of(sprite != null ? sprite.texture : null)
            );
        }
    }
}
