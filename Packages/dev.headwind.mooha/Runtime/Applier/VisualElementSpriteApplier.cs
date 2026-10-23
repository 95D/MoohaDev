using Headwind.Mooha.Provider;
using Headwind.Mooha.Result;
using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.Mooha.Applier
{
    /// <summary>
    /// Applies a loaded Sprite to a VisualElement's backgroundImage.
    /// <para>StyleBackground takes a Sprite directly, which preserves the atlas rect and pivot
    /// metadata that a Texture2D cannot carry.</para>
    /// </summary>
    public class VisualElementSpriteApplier : AsyncResourceApplier<Sprite>
    {
        private readonly VisualElement _target;

        public VisualElementSpriteApplier(
            AsyncResourceProvider<Sprite> provider,
            VisualElement target
        )
            : base(provider)
        {
            _target = target;
        }

        protected override void Apply(ResourceLoadResult<Sprite> result)
        {
            _target.style.backgroundImage = new StyleBackground(result.Value);
        }
    }
}
