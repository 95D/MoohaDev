using System;
using System.Collections.Generic;
using Headwind.Mooha.Size;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Editor.Calculator
{
    /// <summary>
    /// Measures a SpriteAtlas as the sum of its distinct page textures.
    /// <para>Loading any sprite from an atlas brings in every page the atlas packed, so the whole
    /// page set is the resident footprint. GetSprites is the only way to reach those textures:
    /// UnityEditor.U2D.SpriteAtlasExtensions.GetPreviewTextures is a legacy path that does not
    /// promise the final packing result.</para>
    /// </summary>
    public sealed class SpriteAtlasSizeCalculator : IResourceSizeCalculator
    {
        public Type ResourceType => typeof(SpriteAtlas);

        public long Measure(Object asset)
        {
            if (asset is not SpriteAtlas atlas)
                return 0;

            var sprites = new Sprite[atlas.spriteCount];
            var count = atlas.GetSprites(sprites);
            var bytes = SumDistinctPageBytes(sprites, count);
            DestroyClones(sprites, count);
            return bytes;
        }

        /// <summary>
        /// Counts each page once. Every sprite in an atlas points into the same page set, so
        /// measuring per sprite would charge the same bytes repeatedly.
        /// </summary>
        private static long SumDistinctPageBytes(Sprite[] sprites, int count)
        {
            var pages = new HashSet<Texture>();
            var bytes = 0L;
            for (var i = 0; i < count; i++)
            {
                if (sprites[i] == null)
                    continue;
                if (pages.Add(sprites[i].texture))
                    bytes += TextureByteSize.Of(sprites[i].texture);
            }
            return bytes;
        }

        /// <summary>
        /// GetSprites hands back clones that belong to this caller, so they are destroyed here. The
        /// pages they pointed at belong to the atlas and are untouched.
        /// </summary>
        private static void DestroyClones(Sprite[] sprites, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (sprites[i] != null)
                    Object.DestroyImmediate(sprites[i]);
            }
        }
    }
}
