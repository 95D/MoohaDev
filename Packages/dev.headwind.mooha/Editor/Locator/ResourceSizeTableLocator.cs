using System;
using System.Linq;
using Headwind.Mooha.Size;
using UnityEditor;
using UnityEngine;

namespace Headwind.Mooha.Editor.Locator
{
    /// <summary>
    /// Finds the project's single <see cref="ResourceSizeTable"/> asset.
    /// <para>The runtime path reaches the table through a serialized reference while the editor
    /// paths search for it, so two tables in a project would let the two resolve to different ones.
    /// Mooha does not pick a conventional path to avoid forcing a magic folder on its host, and
    /// instead requires that exactly one table exist. Anything else is an error here rather than a
    /// silently empty table at the load path.</para>
    /// </summary>
    public static class ResourceSizeTableLocator
    {
        public static ResourceSizeTable Find()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ResourceSizeTable)}");
            if (guids.Length != 1)
            {
                LogUnexpectedTableCount(guids);
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<ResourceSizeTable>(
                AssetDatabase.GUIDToAssetPath(guids[0])
            );
        }

        /// <summary>
        /// Tells apart the two ways the requirement is missed, because the fix differs.
        /// </summary>
        private static void LogUnexpectedTableCount(string[] guids)
        {
            if (guids.Length == 0)
            {
                Debug.LogError(
                    "[Mooha] No ResourceSizeTable asset exists. Create one through "
                        + "Assets > Create > Mooha > Resource Size Table and assign it to the "
                        + "bootstrap that builds the atlas source."
                );
                return;
            }

            var paths = string.Join(
                ", ",
                guids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path, StringComparer.Ordinal)
            );
            Debug.LogError(
                $"[Mooha] Expected exactly one ResourceSizeTable asset but found {guids.Length}: "
                    + $"{paths}. Delete the extras so every environment reads the same table."
            );
        }
    }
}
