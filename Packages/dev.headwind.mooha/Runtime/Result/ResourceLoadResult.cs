using System;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Result
{
    /// <summary>
    /// One value emitted by a resource load, tagged with which kind of value it is.
    /// <para><see cref="Handle"/> is non-null only when this emission carries a releasable handle,
    /// which the receiver owns until it takes the next emission.</para>
    /// </summary>
    public readonly struct ResourceLoadResult<T>
        where T : Object
    {
        public readonly T Value;
        public readonly ResourceLoadState State;
        public readonly IDisposable Handle;

        public bool IsLoading => State == ResourceLoadState.Placeholder;
        public bool IsSuccess => State == ResourceLoadState.Resource;

        public ResourceLoadResult(T value, ResourceLoadState state, IDisposable handle = null)
        {
            Value = value;
            State = state;
            Handle = handle;
        }
    }
}
