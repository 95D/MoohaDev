using Headwind.Mooha.Request;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Stand-in request that carries nothing but its cache key, for tests that only care which
    /// resource a source was asked for.
    /// <para>Public, and declared outside any test class, because Castle DynamicProxy refuses to
    /// proxy a generic interface closed over a type it cannot see — a substituted
    /// <see cref="Headwind.Mooha.Source.IResourceSource{TRequest,TResource}"/> needs its request
    /// type accessible.</para>
    /// </summary>
    public sealed record FakeRequest<TResource>(string CacheKey) : IResourceRequest<TResource>
        where TResource : Object;
}
