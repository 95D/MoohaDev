using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Headwind.Mooha.Result;
using Headwind.Mooha.Source;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Provider
{
    /// <summary>
    /// Drives one resource load and reports its progress as a stream of results.
    /// <para>Emits Placeholder, then Resource on success or Default on failure. The placeholder is
    /// skipped when the resource resolves without ever suspending — an already resident addressable
    /// or a by-value ref — so rebinding something already on screen does not flash the placeholder
    /// for a frame. Obtain instances from MoohaLoader.CreateProvider().</para>
    /// </summary>
    public class AsyncResourceProvider<T>
        where T : Object
    {
        private readonly ResourceSourceRegistry<T> _sources;
        private readonly T _placeholder;
        private readonly T _default;
        private readonly ResourceRef<T> _failed;

        internal AsyncResourceProvider(
            ResourceSourceRegistry<T> sources,
            T placeholder,
            T defaultResource,
            ResourceRef<T> failed = null
        )
        {
            _sources = sources;
            _placeholder = placeholder;
            _default = defaultResource;
            _failed = failed;
        }

        public IUniTaskAsyncEnumerable<ResourceLoadResult<T>> Load(IResourceRequest<T> request) =>
            UniTaskAsyncEnumerable.Create<ResourceLoadResult<T>>(
                (writer, cancellationToken) => StreamAsync(request, writer, cancellationToken)
            );

        /// <summary>
        /// Cancellation ends the stream without a closing emission, because the consumer that
        /// cancelled is no longer bound to anything this would feed.
        /// </summary>
        private async UniTask StreamAsync(
            IResourceRequest<T> request,
            IAsyncWriter<ResourceLoadResult<T>> writer,
            CancellationToken cancellationToken
        )
        {
            var resolving = Resolve(request, cancellationToken);
            if (WillSuspend(resolving))
                await writer.YieldAsync(
                    new ResourceLoadResult<T>(_placeholder, ResourceLoadState.Placeholder)
                );

            ResourceHandle<T> loaded;
            try
            {
                loaded = await resolving;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            // A source that threw is a wiring or decoding fault, not a missing resource, so it is
            // surfaced rather than quietly becoming a default.
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                loaded = default;
            }

            if (loaded.Value != null)
            {
                await writer.YieldAsync(
                    new ResourceLoadResult<T>(
                        loaded.Value,
                        ResourceLoadState.Resource,
                        loaded.Handle
                    )
                );
                return;
            }

            ResourceHandle<T> fallback;
            try
            {
                fallback = await Resolve(_failed, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await writer.YieldAsync(
                new ResourceLoadResult<T>(
                    fallback.Value ?? _default,
                    ResourceLoadState.Default,
                    fallback.Handle
                )
            );
        }

        private static bool WillSuspend(UniTask<ResourceHandle<T>> resolving) =>
            !resolving.Status.IsCompleted();

        /// <summary>
        /// A null request is a caller mistake rather than an empty load, which every request type
        /// expresses with its own Empty instance; it resolves to nothing so the target still ends
        /// up showing the default.
        /// </summary>
        private UniTask<ResourceHandle<T>> Resolve(
            IResourceRequest<T> request,
            CancellationToken cancellationToken
        ) =>
            request == null
                ? UniTask.FromResult<ResourceHandle<T>>(default)
                : _sources.LoadAsync(request, cancellationToken);

        /// <summary>
        /// A null ref means no fallback was registered, so it resolves to nothing and leaves the
        /// caller its own default.
        /// </summary>
        private async UniTask<ResourceHandle<T>> Resolve(
            ResourceRef<T> resourceRef,
            CancellationToken cancellationToken
        )
        {
            switch (resourceRef)
            {
                case ResourceRef<T>.ByValue byValue:
                    return new ResourceHandle<T>(byValue.Value, null);
                case ResourceRef<T>.ByRequest byRequest:
                    return await Resolve(byRequest.Request, cancellationToken);
                default:
                    return default;
            }
        }
    }
}
