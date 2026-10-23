using Headwind.Mooha.Request;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Model
{
    /// <summary>
    /// What to show: either a value supplied directly or a request left to be loaded.
    /// <para>This is the shape of a fallback, not of a load. A load always takes a request; a
    /// fallback may instead be a resource already in hand, which is the usual case for a baked-in
    /// failure sprite.</para>
    /// </summary>
    public abstract record ResourceRef<T>
        where T : Object
    {
        public sealed record ByValue(T Value) : ResourceRef<T>;

        public sealed record ByRequest(IResourceRequest<T> Request) : ResourceRef<T>;

        public static implicit operator ResourceRef<T>(T value) => new ByValue(value);
    }
}
