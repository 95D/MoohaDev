using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Destroys the Unity objects a test made.
    /// <para>A texture created in a test is not an asset, so nothing collects it on its own and the
    /// editor holds it for the rest of the session.</para>
    /// </summary>
    internal sealed class TrackedObjects
    {
        private readonly List<Object> _objects = new();

        public Texture2D NewTexture() => Track(new Texture2D(1, 1));

        public T Track<T>(T instance)
            where T : Object
        {
            _objects.Add(instance);
            return instance;
        }

        public void DestroyAll()
        {
            foreach (var instance in _objects)
            {
                if (instance != null)
                    Object.DestroyImmediate(instance);
            }
            _objects.Clear();
        }
    }
}
