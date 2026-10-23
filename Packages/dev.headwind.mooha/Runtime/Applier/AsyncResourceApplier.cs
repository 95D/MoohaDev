using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Provider;
using Headwind.Mooha.Request;
using Headwind.Mooha.Result;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Applier
{
    /// <summary>
    /// Keeps one target bound to the latest requested resource.
    /// <para>Override Apply() to define how a result reaches the target. Each emission takes
    /// ownership of its handle and releases the previous one, so the target never outlives the
    /// resource it is showing.</para>
    /// </summary>
    public abstract class AsyncResourceApplier<T> : IDisposable
        where T : Object
    {
        private readonly AsyncResourceProvider<T> _provider;
        private CancellationTokenSource _loadCancellation;
        private IDisposable _currentHandle;
        private bool _disposed;

        protected AsyncResourceApplier(AsyncResourceProvider<T> provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Replaces whichever load is in progress, so it is safe to call on every rebind.
        /// <para>Pass the request type's Empty instance to leave the target on the provider's
        /// default.</para>
        /// </summary>
        public void Load(IResourceRequest<T> request)
        {
            Cancel();
            _loadCancellation = new CancellationTokenSource();
            ApplyEachAsync(request, _loadCancellation.Token).Forget();
        }

        /// <summary>Unlike <see cref="Dispose"/>, leaves this applier reusable.</summary>
        public void Cancel()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
            ReplaceCurrentHandle(null);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Cancel();
        }

        protected abstract void Apply(ResourceLoadResult<T> result);

        private async UniTaskVoid ApplyEachAsync(
            IResourceRequest<T> request,
            CancellationToken cancellationToken
        )
        {
            await foreach (
                var result in _provider.Load(request).WithCancellation(cancellationToken)
            )
            {
                ReplaceCurrentHandle(result.Handle);
                Apply(result);
            }
        }

        private void ReplaceCurrentHandle(IDisposable handle)
        {
            _currentHandle?.Dispose();
            _currentHandle = handle;
        }
    }
}
