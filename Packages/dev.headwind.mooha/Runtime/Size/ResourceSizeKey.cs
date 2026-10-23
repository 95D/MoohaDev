using System;

namespace Headwind.Mooha.Size
{
    /// <summary>
    /// Identifies one baked size entry by the asset's address and the type it is loaded as.
    /// <para>This is deliberately not the cache key: a size is measured while authoring, where
    /// only the asset and its address exist, so it stays reachable no matter which request later
    /// asks for it.</para>
    /// </summary>
    public readonly struct ResourceSizeKey : IEquatable<ResourceSizeKey>
    {
        public readonly Type ResourceType;
        public readonly string Address;

        public ResourceSizeKey(Type resourceType, string address)
        {
            ResourceType = resourceType;
            Address = address;
        }

        public bool Equals(ResourceSizeKey other) =>
            ResourceType == other.ResourceType
            && string.Equals(Address, other.Address, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ResourceSizeKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(ResourceType, Address);
    }
}
