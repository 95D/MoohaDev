using System;
using System.Collections.Generic;
using System.Text;
using Headwind.Mooha.Editor.Calculator;
using Headwind.Mooha.Editor.Locator;
using Headwind.Mooha.Size;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Headwind.Mooha.Editor.Bake
{
    /// <summary>
    /// Measures every measurable Addressable entry and writes the result into the size table.
    /// <para>The population is derived from the Addressable entries rather than a hand-kept list.
    /// Being addressable already states that a resource is loaded lazily through Mooha, so a list
    /// would duplicate that classification and could only drift from it. Scanning all of them is
    /// affordable because the asset type probe reads the database without loading anything.</para>
    /// <para>A missing size degrades to pass-through at runtime, where nothing is wrong enough to
    /// notice, so the bake report is the only place a table problem can surface. That is why an
    /// unmeasurable entry fails the bake instead of being left out of the table.</para>
    /// </summary>
    public static class ResourceSizeTableBaker
    {
        /// <summary>Returns false when the table could not be written or an entry in the population
        /// could not be measured.</summary>
        public static bool Bake()
        {
            var table = ResourceSizeTableLocator.Find();
            if (table == null)
                return false;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError(
                    "[Mooha] No Addressable asset settings in this project, so the resource size "
                        + "table cannot be baked."
                );
                return false;
            }

            var measurement = Measure(settings, table.Addresses);
            Write(table, measurement.Sizes);
            measurement.Report();
            return measurement.Succeeded;
        }

        /// <summary>
        /// Takes the addresses the table already held, so that the ones this pass never reaches
        /// can be named in the report.
        /// </summary>
        private static Measurement Measure(
            AddressableAssetSettings settings,
            IEnumerable<string> bakedAddresses
        )
        {
            var calculators = ResourceSizeCalculatorRegistry.Resolve();
            var measurement = new Measurement(bakedAddresses);
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;
                foreach (var entry in group.entries)
                    MeasureEntry(entry, calculators, measurement);
            }
            return measurement;
        }

        /// <summary>
        /// An entry whose asset type has no calculator is outside the population altogether, so it
        /// leaves no trace; the rest are measured or recorded as a failure.
        /// </summary>
        private static void MeasureEntry(
            AddressableAssetEntry entry,
            Dictionary<Type, IResourceSizeCalculator> calculators,
            Measurement measurement
        )
        {
            if (entry == null || string.IsNullOrEmpty(entry.address))
                return;

            var assetPath = entry.AssetPath;
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                measurement.Folders.Add(entry.address);
                return;
            }

            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == null || !calculators.TryGetValue(assetType, out var calculator))
                return;

            measurement.Reached(entry.address);

            var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
            var bytes = asset == null ? 0 : calculator.Measure(asset);
            if (bytes <= 0)
            {
                measurement.Failures.Add($"{entry.address} ({assetPath})");
                return;
            }
            measurement.Sizes[new ResourceSizeKey(assetType, entry.address)] = bytes;
        }

        /// <summary>
        /// The bake owns the table outright, so the measured population replaces its contents and
        /// is saved here rather than left dirty for whatever runs next.
        /// </summary>
        private static void Write(ResourceSizeTable table, Dictionary<ResourceSizeKey, long> sizes)
        {
            table.Bake(sizes);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);
        }

        /// <summary>
        /// What one pass over the Addressable entries found: the sizes to write, and everything
        /// about the pass that the report has to mention.
        /// </summary>
        private sealed class Measurement
        {
            public readonly Dictionary<ResourceSizeKey, long> Sizes = new();

            /// <summary>
            /// Entries in the population whose calculator returned nothing usable.
            /// </summary>
            public readonly List<string> Failures = new();

            /// <summary>
            /// Folder entries, which give their children implicit addresses composed from the
            /// parent. Composing those is unimplemented, so folders are collected to keep the gap
            /// from being silent.
            /// </summary>
            public readonly List<string> Folders = new();

            /// <summary>
            /// Addresses the table held before this pass that the pass never reached, so code still
            /// composing them now loads nothing.
            /// </summary>
            public readonly HashSet<string> DroppedAddresses;

            private int _population;

            public Measurement(IEnumerable<string> bakedAddresses)
            {
                DroppedAddresses = new HashSet<string>(bakedAddresses, StringComparer.Ordinal);
            }

            public bool Succeeded => Failures.Count == 0;

            /// <summary>
            /// Counts an address as part of the population. The address still exists even if
            /// measuring it then fails, so reaching it is what clears it from
            /// <see cref="DroppedAddresses"/>.
            /// </summary>
            public void Reached(string address)
            {
                _population++;
                DroppedAddresses.Remove(address);
            }

            public void Report()
            {
                var report = new StringBuilder();
                report.Append(
                    "[Mooha] Resource size table baked for "
                        + $"{EditorUserBuildSettings.activeBuildTarget}: {Sizes.Count}/"
                        + $"{_population} measured."
                );
                if (Failures.Count > 0)
                    report.Append($"\nCould not measure: {string.Join(", ", Failures)}");
                if (Folders.Count > 0)
                {
                    report.Append(
                        "\nSkipped folder entries, whose children are never measured: "
                            + $"{string.Join(", ", Folders)}"
                    );
                }
                if (DroppedAddresses.Count > 0)
                {
                    report.Append(
                        "\nPreviously baked addresses that no longer exist, so code still "
                            + "composing them loads nothing: "
                            + $"{string.Join(", ", DroppedAddresses)}"
                    );
                }

                if (Failures.Count > 0)
                    Debug.LogError(report.ToString());
                else if (Folders.Count > 0 || DroppedAddresses.Count > 0)
                    Debug.LogWarning(report.ToString());
                else
                    Debug.Log(report.ToString());
            }
        }
    }
}
