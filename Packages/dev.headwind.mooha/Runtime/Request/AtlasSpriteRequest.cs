using UnityEngine;

namespace Headwind.Mooha.Request
{
    /// <summary>
    /// Asks for one sprite packed into the SpriteAtlas addressable at <c>AtlasAddress</c>.
    /// <para>The atlas and the sprite name stay apart because they are resolved by different
    /// layers: the atlas is what gets loaded and cached, the name only selects a rect inside it.
    /// </para>
    /// </summary>
    public sealed record AtlasSpriteRequest(string AtlasAddress, string SpriteName)
        : IResourceRequest<Sprite>
    {
        /// <summary>Loads nothing, for a target that should show the provider's default.</summary>
        public static readonly AtlasSpriteRequest Empty = new(string.Empty, string.Empty);

        /// <summary>
        /// Matches the Addressables sub-object form, which is also what a direct Addressables
        /// caller would use as its key, so the two never disagree about identity.
        /// </summary>
        public string CacheKey => $"{AtlasAddress}[{SpriteName}]";
    }
}
