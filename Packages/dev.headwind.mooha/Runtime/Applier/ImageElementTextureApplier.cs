using Headwind.Mooha.Provider;
using Headwind.Mooha.Result;
using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.Mooha.Applier
{
    /// <summary>
    /// Applies a loaded Texture2D to an <see cref="Image"/> element's <c>image</c> property.
    /// <para>Assigning <c>image</c> rather than a background honors the element's explicit
    /// width/height and scaleMode instead of relying on background-size.</para>
    /// </summary>
    public class ImageElementTextureApplier : AsyncResourceApplier<Texture2D>
    {
        private readonly Image _target;

        public ImageElementTextureApplier(AsyncResourceProvider<Texture2D> provider, Image target)
            : base(provider)
        {
            _target = target;
        }

        protected override void Apply(ResourceLoadResult<Texture2D> result)
        {
            _target.image = result.Value;
        }
    }
}
