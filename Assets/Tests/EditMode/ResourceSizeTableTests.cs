using System.Collections.Generic;
using System.Text.RegularExpressions;
using Headwind.Mooha.Size;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Covers the bake-to-runtime round trip, where the table has to survive a type that cannot be
    /// serialized and a lookup that is built once.
    /// </summary>
    public class ResourceSizeTableTests
    {
        private readonly TrackedObjects _objects = new();

        [TearDown]
        public void DestroyTrackedObjects() => _objects.DestroyAll();

        [Test]
        public void ABakedSizeIsReadBackByBothTypeAndAddress()
        {
            var table = NewTable();

            table.Bake(
                new Dictionary<ResourceSizeKey, long>
                {
                    [new ResourceSizeKey(typeof(Texture2D), "icons/sword")] = 4096,
                }
            );

            Assert.That(table.TryGetBytes(typeof(Texture2D), "icons/sword", out var bytes), Is.True);
            Assert.That(bytes, Is.EqualTo(4096));
            Assert.That(table.TryGetBytes(typeof(Sprite), "icons/sword", out _), Is.False);
            Assert.That(table.TryGetBytes(typeof(Texture2D), "icons/shield", out _), Is.False);
            Assert.That(table.Addresses, Is.EquivalentTo(new[] { "icons/sword" }));
        }

        /// <summary>
        /// The bake owns the whole population, so a second one must not leave entries behind — nor
        /// answer out of the lookup the first query built.
        /// </summary>
        [Test]
        public void ARebakeReplacesEverythingTheTableHeld()
        {
            var table = NewTable();
            table.Bake(
                new Dictionary<ResourceSizeKey, long>
                {
                    [new ResourceSizeKey(typeof(Texture2D), "icons/sword")] = 4096,
                }
            );
            table.TryGetBytes(typeof(Texture2D), "icons/sword", out _);

            table.Bake(
                new Dictionary<ResourceSizeKey, long>
                {
                    [new ResourceSizeKey(typeof(Texture2D), "icons/shield")] = 8192,
                }
            );

            Assert.That(table.TryGetBytes(typeof(Texture2D), "icons/sword", out _), Is.False);
            Assert.That(
                table.TryGetBytes(typeof(Texture2D), "icons/shield", out var bytes),
                Is.True
            );
            Assert.That(bytes, Is.EqualTo(8192));
        }

        /// <summary>
        /// A stale entry is dropped at the cost of a cache miss rather than answering with a size
        /// measured for something else. The bake can only write type names that resolve, so the
        /// entry is injected through serialization the way a table baked against an assembly that
        /// has since been removed would come back.
        /// </summary>
        [Test]
        public void AnEntryWhoseTypeNoLongerResolvesIsDropped()
        {
            var table = NewTable();
            JsonUtility.FromJsonOverwrite(
                "{\"_entries\":[{\"TypeName\":\"Removed.Type, Removed.Assembly\","
                    + "\"Address\":\"icons/sword\",\"Bytes\":4096}]}",
                table
            );

            LogAssert.Expect(LogType.Warning, new Regex("icons/sword"));

            Assert.That(table.TryGetBytes(typeof(Texture2D), "icons/sword", out _), Is.False);
        }

        private ResourceSizeTable NewTable() =>
            _objects.Track(ScriptableObject.CreateInstance<ResourceSizeTable>());
    }
}
