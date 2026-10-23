using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Cache;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Serves requests out of a shared cache, falling through to the wrapped source on a miss.
    /// <para>A result reporting no cost is passed straight through without an entry. That covers
    /// anything whose memory is already charged to a lower layer, such as a sprite cloned out of a
    /// cached atlas: giving it an entry of its own would both double-count the atlas texture and
    /// put several entries in front of one shared resource, which is the arrangement the atlas
    /// layer exists to avoid.</para>
    /// </summary>
    public sealed class CachedResourceSource<TRequest, TResource>
        : IResourceSource<TRequest, TResource>
        where TRequest : IResourceRequest<TResource>
        where TResource : Object
    {
        private readonly IResourceSource<TRequest, TResource> _source;
        private readonly ResourceCacheStore _cache;

        public CachedResourceSource(
            IResourceSource<TRequest, TResource> source,
            ResourceCacheStore cache
        )
        {
            _source = source;
            _cache = cache;
        }

        public async UniTask<ResourceHandle<TResource>> LoadAsync(
            TRequest request,
            CancellationToken cancellationToken
        )
        {
            var key = new ResourceCacheKey(typeof(TRequest), request.CacheKey);
            if (_cache.TryAcquire<TResource>(key, out var cached))
                return cached;

            var loaded = await _source.LoadAsync(request, cancellationToken);
            if (!IsCacheable(loaded))
                return loaded;

            return _cache.Admit(key, loaded);
        }

        private static bool IsCacheable(ResourceHandle<TResource> loaded) =>
            loaded.Value != null && loaded.Handle != null && loaded.Cost > 0;
    }
}
