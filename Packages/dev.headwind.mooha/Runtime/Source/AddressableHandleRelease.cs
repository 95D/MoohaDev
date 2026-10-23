using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Releases one Addressables handle when disposed.
    /// </summary>
    internal sealed class AddressableHandleRelease<T> : IDisposable
    {
        private AsyncOperationHandle<T> _handle;

        public AddressableHandleRelease(AsyncOperationHandle<T> handle)
        {
            _handle = handle;
        }

        public void Dispose()
        {
            if (_handle.IsValid())
                Addressables.Release(_handle);
            _handle = default;
        }
    }
}
