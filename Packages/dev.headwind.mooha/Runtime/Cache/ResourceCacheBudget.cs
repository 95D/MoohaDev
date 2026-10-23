using System;
using UnityEngine;

namespace Headwind.Mooha.Cache
{
    /// <summary>
    /// How many bytes of released resources a cache may keep before it starts evicting.
    /// <para>The default scales with device RAM rather than with the managed heap. Coil sizes its
    /// cache as a share of the Android heap because bitmap pixels live there, but Unity keeps
    /// texture memory outside the managed heap, so that base would measure the wrong pool.</para>
    /// <para>The ratio and clamps below are a conservative starting point, not a measured one.
    /// </para>
    /// </summary>
    public readonly struct ResourceCacheBudget
    {
        private const float DefaultDeviceMemoryRatio = 0.02f;
        private const long MinimumBytes = 16L * 1024 * 1024;
        private const long MaximumBytes = 128L * 1024 * 1024;
        private const long BytesPerMegabyte = 1024 * 1024;

        public readonly long MaxBytes;

        public ResourceCacheBudget(long maxBytes)
        {
            MaxBytes = Math.Max(0, maxBytes);
        }

        public static ResourceCacheBudget FromDeviceMemory(
            float ratio = DefaultDeviceMemoryRatio,
            long minimumBytes = MinimumBytes,
            long maximumBytes = MaximumBytes
        )
        {
            var deviceBytes = (long)SystemInfo.systemMemorySize * BytesPerMegabyte;
            var scaled = (long)(deviceBytes * ratio);
            return new ResourceCacheBudget(Math.Clamp(scaled, minimumBytes, maximumBytes));
        }
    }
}
