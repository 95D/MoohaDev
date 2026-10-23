using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Loads an Addressable asset while honoring cancellation.
    /// <para>A failure comes back as an invalid handle rather than an exception, so a caller only
    /// has to inspect the result. On success the handle is handed back live for the caller to own.
    /// </para>
    /// </summary>
    internal static class AddressableHandleLoader
    {
        public static async UniTask<AsyncOperationHandle<T>> LoadAsync<T>(
            string address,
            CancellationToken cancellationToken
        )
        {
            var handle = default(AsyncOperationHandle<T>);
            var registration = default(CancellationTokenRegistration);
            try
            {
                handle = Addressables.LoadAssetAsync<T>(address);
                registration = cancellationToken.Register(() =>
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                });
                await handle.ToUniTask(cancellationToken: cancellationToken);
                // Unregistered before the handle leaves — cancellation must not release it anymore.
                registration.Dispose();
                return handle;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
                return default;
            }
            finally
            {
                registration.Dispose();
            }
        }
    }
}
