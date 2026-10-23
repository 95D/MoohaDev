using System;

namespace Headwind.Mooha.Cache
{
    /// <summary>
    /// Identifies one cache entry by the request that produced it and the key that request reports.
    /// <para>The request type belongs in the key because a single store holds every resource type
    /// against one budget. Keying on the request type rather than the resource type also keeps two
    /// sources that happen to name their resources alike — a local address and a remote path that
    /// read the same — in separate entries without either having to namespace its keys.</para>
    /// </summary>
    public readonly struct ResourceCacheKey : IEquatable<ResourceCacheKey>
    {
        public readonly Type RequestType;
        public readonly string Key;

        public ResourceCacheKey(Type requestType, string key)
        {
            RequestType = requestType;
            Key = key;
        }

        public bool Equals(ResourceCacheKey other) =>
            RequestType == other.RequestType
            && string.Equals(Key, other.Key, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ResourceCacheKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(RequestType, Key);
    }
}
