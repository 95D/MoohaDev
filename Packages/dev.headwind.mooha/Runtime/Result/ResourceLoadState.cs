namespace Headwind.Mooha.Result
{
    /// <summary>
    /// Which kind of value a resource load emitted.
    /// </summary>
    public enum ResourceLoadState
    {
        /// <summary>Stand-in emitted while the requested resource is still loading.</summary>
        Placeholder,

        /// <summary>The requested resource.</summary>
        Resource,

        /// <summary>Fallback emitted because the requested resource could not be loaded.</summary>
        Default,
    }
}
