using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Headwind.Mooha.Size;
using UnityEngine;

namespace Headwind.Mooha.Source
{
    /// <summary>Loads Texture2Ds through Addressable Assets.</summary>
    public class AddressableTextureSource : IResourceSource<AddressableTextureRequest, Texture2D>
    {
        private readonly AddressableKeyExistence _keyExistence = new();

        public async UniTask<ResourceHandle<Texture2D>> LoadAsync(
            AddressableTextureRequest request,
            CancellationToken cancellationToken
        )
        {
            if (!await _keyExistence.ExistsAsync(request.Address, cancellationToken))
                return default;

            var handle = await AddressableHandleLoader.LoadAsync<Texture2D>(
                request.Address,
                cancellationToken
            );
            if (!handle.IsValid())
                return default;

            var texture = handle.Result;
            return new ResourceHandle<Texture2D>(
                texture,
                new AddressableHandleRelease<Texture2D>(handle),
                TextureByteSize.Of(texture)
            );
        }
    }
}
