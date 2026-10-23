using System.Threading;
using Cysharp.Threading.Tasks;
using Headwind.Mooha.Model;
using Headwind.Mooha.Source;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Headwind.Mooha.Tests
{
    /// <summary>
    /// Proves the harness itself works: NSubstitute can proxy a Mooha contract whose return type is
    /// a UniTask, and an awaited substitute completes synchronously under the test runner.
    /// </summary>
    public class SubstituteSmokeTests
    {
        [Test]
        public void SubstitutedSourceReturnsTheConfiguredHandle()
        {
            var texture = new Texture2D(1, 1);
            var source = Substitute.For<IResourceSource<FakeRequest<Texture2D>, Texture2D>>();
            source
                .LoadAsync(Arg.Any<FakeRequest<Texture2D>>(), Arg.Any<CancellationToken>())
                .Returns(UniTask.FromResult(new ResourceHandle<Texture2D>(texture, null, 1)));

            var handle = source
                .LoadAsync(new FakeRequest<Texture2D>("smoke"), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(handle.Value, Is.SameAs(texture));
            Assert.That(handle.Cost, Is.EqualTo(1));

            Object.DestroyImmediate(texture);
        }
    }
}
