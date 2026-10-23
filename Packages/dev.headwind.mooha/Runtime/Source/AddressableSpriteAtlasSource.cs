using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Headwind.Mooha.Size;
using UnityEngine;
using UnityEngine.U2D;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Loads SpriteAtlases through Addressable Assets.
    /// <para>Sprites packed into an atlas all draw from the same page textures, so the atlas — not
    /// the sprite — is what actually occupies memory and what actually frees it. Loading atlases
    /// through their own source lets a cache sit at that level, where an entry's reported cost
    /// matches the bytes its eviction returns.</para>
    /// <para>The cost comes from <see cref="IResourceSizeTable"/> rather than from the atlas, which
    /// exposes no way to enumerate its pages at runtime. An address the table does not know reports
    /// zero, which the cache layer reads as pass-through: the atlas loads but never enters the
    /// budget, so a stale table costs reloads and never wrong accounting.</para>
    /// </summary>
    public class AddressableSpriteAtlasSource
        : IResourceSource<AddressableSpriteAtlasRequest, SpriteAtlas>
    {
        private readonly IResourceSizeTable _sizeTable;
        private readonly AddressableKeyExistence _keyExistence = new();
        private readonly HashSet<string> _unknownSizeAddresses = new(StringComparer.Ordinal);

        public AddressableSpriteAtlasSource(IResourceSizeTable sizeTable)
        {
            _sizeTable = sizeTable ?? throw new ArgumentNullException(nameof(sizeTable));
        }

        public async UniTask<ResourceHandle<SpriteAtlas>> LoadAsync(
            AddressableSpriteAtlasRequest request,
            CancellationToken cancellationToken
        )
        {
            if (!await _keyExistence.ExistsAsync(request.Address, cancellationToken))
                return default;

            var handle = await AddressableHandleLoader.LoadAsync<SpriteAtlas>(
                request.Address,
                cancellationToken
            );
            if (!handle.IsValid())
                return default;

            return new ResourceHandle<SpriteAtlas>(
                handle.Result,
                new AddressableHandleRelease<SpriteAtlas>(handle),
                ResolveCostBytes(request.Address)
            );
        }

        /// <summary>
        /// The bake hooks make a miss nearly unreachable, so one warning per address is enough to
        /// surface a table that was never baked without flooding the log from the load path.
        /// </summary>
        private long ResolveCostBytes(string address)
        {
            if (_sizeTable.TryGetBytes(typeof(SpriteAtlas), address, out var bytes))
                return bytes;
            if (_unknownSizeAddresses.Add(address))
            {
                Debug.LogWarning(
                    $"[Mooha] No determined size for atlas '{address}', so it stays out of the "
                        + "cache. Re-bake the resource size table."
                );
            }
            return 0;
        }
    }
}
