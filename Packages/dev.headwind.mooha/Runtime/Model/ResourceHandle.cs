using System;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Model
{
    /// <summary>
    /// Pairs a loaded resource with the IDisposable that releases its underlying handle.
    /// <para>Handle must be disposed once the resource is no longer in use. It is null for a
    /// resource that is not backed by a releasable handle, such as a
    /// <see cref="ResourceRef{T}.ByValue"/> ref.</para>
    /// <para>Cost is what the resource occupies in bytes, which a cache spends against its budget.
    /// Each source reports its own, since no general measurement fits every resource — a managed
    /// Texture2D or Sprite is a pointer-sized shell over native memory. Zero means this resource is
    /// not separately chargeable: either nothing was allocated for it, or the memory it occupies is
    /// already accounted for elsewhere, as with a sprite cloned out of a cached atlas.</para>
    /// </summary>
    public readonly struct ResourceHandle<T>
        where T : Object
    {
        public readonly T Value;
        public readonly IDisposable Handle;
        public readonly long Cost;

        public ResourceHandle(T value, IDisposable handle, long cost = 0)
        {
            Value = value;
            Handle = handle;
            Cost = cost;
        }
    }
}
