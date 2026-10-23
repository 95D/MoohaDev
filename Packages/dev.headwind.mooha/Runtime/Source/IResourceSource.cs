using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Request;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Source
{
    /// <summary>
    /// Serves one kind of request, producing a resource of type <typeparamref name="TResource"/>.
    /// <para>A request it cannot resolve yields default instead of throwing, so a missing resource
    /// is a value the caller inspects — including a request whose parameters are empty, which each
    /// source rejects on its own terms rather than being screened in advance. Ownership of the
    /// returned handle passes to the caller, which must dispose it once the resource is no longer
    /// in use.</para>
    /// </summary>
    public interface IResourceSource<in TRequest, TResource>
        where TRequest : IResourceRequest<TResource>
        where TResource : Object
    {
        UniTask<ResourceHandle<TResource>> LoadAsync(
            TRequest request,
            CancellationToken cancellationToken
        );
    }
}
