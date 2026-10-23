using System;
using System.Collections.Generic;
using Headwind.Mooha.Model;
using Headwind.Mooha.Provider;
using Headwind.Mooha.Request;
using Headwind.Mooha.Source;
using Object = UnityEngine.Object;

namespace Headwind.Mooha
{
    /// <summary>
    /// Global entry point for the Mooha resource loading library.
    /// <para>Call RegisterSource() once per request type and SetDefaults() once per resource type
    /// at app startup, then CreateProvider&lt;T&gt;() wherever a load is needed.</para>
    /// </summary>
    public static class MoohaLoader
    {
        private static readonly Dictionary<Type, object> Configs = new();

        /// <summary>
        /// Registration is per request type and last write wins, so each entry point that
        /// initializes — play mode start, editor domain reload — replaces the sources it knows
        /// about with freshly built ones and drops whatever state those had accumulated.
        /// </summary>
        public static void RegisterSource<TRequest, TResource>(
            IResourceSource<TRequest, TResource> source
        )
            where TRequest : IResourceRequest<TResource>
            where TResource : Object
        {
            GetOrCreateConfig<TResource>().Sources.Register(source);
        }

        /// <summary>
        /// Registers the fallbacks every provider for <typeparamref name="TResource"/> uses when a
        /// load has not finished or has failed.
        /// <para>These sit on the resource rather than the request: what a target shows while
        /// waiting is a property of what it displays, not of where the resource was being fetched
        /// from.</para>
        /// </summary>
        public static void SetDefaults<TResource>(
            TResource placeholder = null,
            TResource defaultResource = null,
            ResourceRef<TResource> failed = null
        )
            where TResource : Object
        {
            var config = GetOrCreateConfig<TResource>();
            config.Placeholder = placeholder;
            config.Default = defaultResource;
            config.Failed = failed;
        }

        /// <summary>
        /// Every argument left null takes the value registered by
        /// <see cref="SetDefaults{TResource}"/>.
        /// </summary>
        public static AsyncResourceProvider<TResource> CreateProvider<TResource>(
            TResource placeholder = null,
            TResource defaultResource = null,
            ResourceRef<TResource> failed = null
        )
            where TResource : Object
        {
            var config = GetRegisteredConfig<TResource>();
            return new AsyncResourceProvider<TResource>(
                config.Sources,
                placeholder ?? config.Placeholder,
                defaultResource ?? config.Default,
                failed ?? config.Failed
            );
        }

        private static Config<TResource> GetOrCreateConfig<TResource>()
            where TResource : Object
        {
            if (Configs.TryGetValue(typeof(TResource), out var boxed))
                return (Config<TResource>)boxed;

            var config = new Config<TResource>();
            Configs[typeof(TResource)] = config;
            return config;
        }

        /// <summary>
        /// Throws unless a source has already been registered, because a provider with no source
        /// could only ever emit the default and would hide the missing wiring.
        /// </summary>
        private static Config<TResource> GetRegisteredConfig<TResource>()
            where TResource : Object
        {
            var config = Configs.TryGetValue(typeof(TResource), out var boxed)
                ? (Config<TResource>)boxed
                : null;
            if (config == null || config.Sources.IsEmpty)
                throw new InvalidOperationException(
                    $"MoohaLoader.RegisterSource for {typeof(TResource).Name} must be called "
                        + "before CreateProvider."
                );
            return config;
        }

        private sealed class Config<T>
            where T : Object
        {
            public readonly ResourceSourceRegistry<T> Sources = new();
            public T Placeholder;
            public T Default;
            public ResourceRef<T> Failed;
        }
    }
}
