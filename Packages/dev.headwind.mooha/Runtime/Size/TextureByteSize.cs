using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Headwind.Mooha.Size
{
    /// <summary>
    /// Measures what a texture occupies in memory, for use as a cache weight.
    /// <para>The size is computed from the texture's format and dimensions rather than read back
    /// from UnityEngine.Profiling.Profiler. Profiler's query methods are gated on ENABLE_PROFILER,
    /// which is defined only in the editor and in development builds; a release player compiles
    /// them away and they return zero without failing. A byte-weighted cache fed those zeros would
    /// believe every entry is free and grow without bound.</para>
    /// </summary>
    public static class TextureByteSize
    {
        public static long Of(Texture texture)
        {
            if (texture == null)
                return 0;
            return GraphicsFormatUtility.ComputeMipChainSize(
                texture.width,
                texture.height,
                texture.graphicsFormat,
                texture.mipmapCount
            );
        }
    }
}
