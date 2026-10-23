using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Answers whether an Addressables key exists, remembering answers it has already resolved.
    /// <para>A location lookup is a brand new async operation every time, so it never completes
    /// synchronously and costs a frame even when the asset itself is already resident — which makes
    /// an otherwise cache-hit load emit a placeholder. Remembering the answer lets such a load
    /// finish inside the calling frame. A resource source owns one of these, and sources are
    /// rebuilt on every MoohaLoader.RegisterSource, so no answer outlives a catalog change.</para>
    /// </summary>
    internal sealed class AddressableKeyExistence
    {
        private readonly Dictionary<string, bool> _resolved = new();

        public UniTask<bool> ExistsAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key))
                return UniTask.FromResult(false);
            return _resolved.TryGetValue(key, out var known)
                ? UniTask.FromResult(known)
                : QueryAsync(key, cancellationToken);
        }

        private async UniTask<bool> QueryAsync(string key, CancellationToken cancellationToken)
        {
            var handle = Addressables.LoadResourceLocationsAsync(key);
            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
                var exists = handle.Result != null && handle.Result.Count > 0;
                _resolved[key] = exists;
                return exists;
            }
            // A lookup that threw is inconclusive, so it reports missing without being remembered.
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return false;
            }
            finally
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }
    }
}
