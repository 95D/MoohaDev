using Headwind.Mooha.Provider;
using Headwind.Mooha.Result;
using UnityEngine;

namespace Headwind.Mooha.Applier
{
    /// <summary>
    /// Applies a loaded Texture2D to a SpriteRenderer, wrapped in a full-rect centred Sprite.
    /// <para>A SpriteRenderer draws a Sprite rather than a texture, so one is built per emission. A
    /// null value is left alone, which keeps the renderer on whatever it last showed instead of
    /// blanking it.</para>
    /// </summary>
    public class SpriteRendererTextureApplier : AsyncResourceApplier<Texture2D>
    {
        private readonly SpriteRenderer _target;

        public SpriteRendererTextureApplier(
            AsyncResourceProvider<Texture2D> provider,
            SpriteRenderer target
        )
            : base(provider)
        {
            _target = target;
        }

        protected override void Apply(ResourceLoadResult<Texture2D> result)
        {
            if (result.Value == null)
                return;
            var texture = result.Value;
            _target.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
    }
}
