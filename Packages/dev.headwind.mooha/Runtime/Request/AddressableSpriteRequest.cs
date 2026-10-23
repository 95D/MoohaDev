using UnityEngine;

namespace Headwind.Mooha.Request
{
    /// <summary>Asks for a standalone Sprite addressable at <c>Address</c>.</summary>
    public sealed record AddressableSpriteRequest(string Address) : IResourceRequest<Sprite>
    {
        /// <summary>Loads nothing, for a target that should show the provider's default.</summary>
        public static readonly AddressableSpriteRequest Empty = new(string.Empty);

        public string CacheKey => Address;
    }
}
