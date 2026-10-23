using Headwind.Mooha.Provider;
using Headwind.Mooha.Result;
using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.Mooha.Applier
{
    /// <summary>
    /// Applies a loaded Texture2D to a VisualElement's backgroundImage.
    /// <para>This is the choice for an element laid out by its background rules.
    /// <see cref="ImageElementTextureApplier"/> is the one to use when the element's own
    /// width/height and scaleMode should govern instead.</para>
    /// </summary>
    public class VisualElementTextureApplier : AsyncResourceApplier<Texture2D>
    {
        private readonly VisualElement _target;

        public VisualElementTextureApplier(
            AsyncResourceProvider<Texture2D> provider,
            VisualElement target
        )
            : base(provider)
        {
            _target = target;
        }

        protected override void Apply(ResourceLoadResult<Texture2D> result)
        {
            _target.style.backgroundImage = new StyleBackground(result.Value);
        }
    }
}
