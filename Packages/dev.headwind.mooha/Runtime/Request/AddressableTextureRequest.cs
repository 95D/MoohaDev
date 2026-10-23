using UnityEngine;

namespace Headwind.Mooha.Request
{
    /// <summary>Asks for a standalone Texture2D addressable at <c>Address</c>.</summary>
    public sealed record AddressableTextureRequest(string Address) : IResourceRequest<Texture2D>
    {
        /// <summary>Loads nothing, for a target that should show the provider's default.</summary>
        public static readonly AddressableTextureRequest Empty = new(string.Empty);

        public string CacheKey => Address;
    }
}
