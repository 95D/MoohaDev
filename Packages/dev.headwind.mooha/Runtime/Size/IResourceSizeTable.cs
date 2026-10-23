using System;

namespace Headwind.Mooha.Size
{
    /// <summary>
    /// Supplies resource byte sizes determined while authoring instead of measured on load.
    /// <para>Some resources cannot report their own residency. A SpriteAtlas is the standing case:
    /// the page textures are what occupy memory, and the runtime type offers no way to enumerate
    /// them. Determining those sizes up front keeps the enumeration — main thread only, and
    /// proportional to the sprite count — out of the load path.</para>
    /// <para>The key is the resource type and the address it was authored at, which is what the
    /// bake step can see, so the table stays independent of whichever request asks for it. A
    /// missing key is not an error: the caller reports an unknown cost, which keeps the resource
    /// out of the byte budget rather than charging it a guess.</para>
    /// </summary>
    public interface IResourceSizeTable
    {
        bool TryGetBytes(Type resourceType, string address, out long bytes);
    }
}
