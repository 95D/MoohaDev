using System;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Editor.Calculator
{
    /// <summary>
    /// Measures how many bytes one asset occupies once loaded, for the size table bake.
    /// <para>A pure asset-to-bytes function: the bake resolves addresses and writes the table, so
    /// an implementation only needs to know how its own asset type spends memory and never sees
    /// Addressables. Mooha ships the SpriteAtlas calculator because atlas pages cannot be measured
    /// at runtime at all; types whose byte accounting is a project policy, such as prefabs or asset
    /// catalogs, are left to the project to implement.</para>
    /// <para>One asset yields one entry. An asset that drags several Unity objects into memory is
    /// summed into a single number here, because the load and eviction unit is the address.</para>
    /// <para>Implementations are discovered by type, so they need a parameterless constructor, and
    /// only one may claim a given <see cref="ResourceType"/>. Returning zero or less marks the
    /// measurement as failed and the bake reports it.</para>
    /// </summary>
    public interface IResourceSizeCalculator
    {
        /// <summary>
        /// The asset's main type, which is also the type the runtime source requests.
        /// </summary>
        Type ResourceType { get; }

        long Measure(Object asset);
    }
}
