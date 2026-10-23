using System;
using NUnit.Framework;
using UnityEngine;

namespace Headwind.Mooha.Tests
{
    public class MoohaLoaderTests
    {
        /// <summary>
        /// A provider with no source could only ever emit the default, which would hide the missing
        /// registration behind a target that looks merely unloaded.
        /// <para>Material is the one resource type no other test registers a source for.</para>
        /// </summary>
        [Test]
        public void CreatingAProviderBeforeAnySourceIsRegisteredThrows()
        {
            Assert.Throws<InvalidOperationException>(
                () => MoohaLoader.CreateProvider<Material>()
            );
        }
    }
}
