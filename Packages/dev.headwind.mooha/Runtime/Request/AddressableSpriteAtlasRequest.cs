using UnityEngine.U2D;

namespace Headwind.Mooha.Request
{
    /// <summary>Asks for a SpriteAtlas addressable at <c>Address</c>.</summary>
    public sealed record AddressableSpriteAtlasRequest(string Address)
        : IResourceRequest<SpriteAtlas>
    {
        /// <summary>Loads nothing, for a target that should show the provider's default.</summary>
        public static readonly AddressableSpriteAtlasRequest Empty = new(string.Empty);

        public string CacheKey => Address;
    }
}
