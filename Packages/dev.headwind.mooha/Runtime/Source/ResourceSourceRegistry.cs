using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Holds every source registered for one resource type, keyed by the request each one serves.
    /// <para>A source is typed on its request while a provider is typed only on its resource, so
    /// the request's runtime type is what picks the source. Registration captures the typed source
    /// in a closure, which is where the cast back to the concrete request happens — the last point
    /// at which that type is still known statically.</para>
    /// </summary>
    public sealed class ResourceSourceRegistry<TResource>
        where TResource : Object
    {
        private delegate UniTask<ResourceHandle<TResource>> Loader(
            IResourceRequest<TResource> request,
            CancellationToken cancellationToken
        );

        private readonly Dictionary<Type, Loader> _sources = new();

        public bool IsEmpty => _sources.Count == 0;

        /// <summary>Replaces whichever source held <typeparamref name="TRequest"/> before.
        /// </summary>
        public void Register<TRequest>(IResourceSource<TRequest, TResource> source)
            where TRequest : IResourceRequest<TResource>
        {
            _sources[typeof(TRequest)] = (request, cancellationToken) =>
                source.LoadAsync((TRequest)request, cancellationToken);
        }

        public UniTask<ResourceHandle<TResource>> LoadAsync(
            IResourceRequest<TResource> request,
            CancellationToken cancellationToken
        )
        {
            if (!_sources.TryGetValue(request.GetType(), out var loader))
                throw new InvalidOperationException(
                    $"No Mooha source is registered for {request.GetType().Name}. Call "
                        + $"MoohaLoader.RegisterSource<{request.GetType().Name}, "
                        + $"{typeof(TResource).Name}>() at startup."
                );
            return loader(request, cancellationToken);
        }
    }
}
