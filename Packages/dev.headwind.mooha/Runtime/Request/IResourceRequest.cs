using Object = UnityEngine.Object;

namespace Headwind.Mooha.Request
{
    /// <summary>
    /// Describes one resource to load, pinning the type that load resolves to.
    /// <para>A request carries whatever parameters its source needs — an address, a URL, a
    /// sub-object name — so a source is selected by what is being asked for rather than by the type
    /// coming back. Two sources can therefore both produce a Texture2D while taking entirely
    /// different inputs.</para>
    /// <para><typeparamref name="TResource"/> appears in no member: it exists to make a request and
    /// its resource type inseparable at the call site, so a request can only be handed to a
    /// provider that yields the matching resource.</para>
    /// </summary>
    public interface IResourceRequest<TResource>
        where TResource : Object
    {
        /// <summary>
        /// Identifies the resource this request resolves to, for callers that cache by request.
        /// <para>Only the parameters that decide which resource comes back belong here. Anything
        /// describing how to fetch it — a timeout, a priority — must stay out, or two requests for
        /// one resource would occupy two cache entries.</para>
        /// </summary>
        string CacheKey { get; }
    }
}
