using NUnit.Framework;
using TPS.Runtime.World;
using UnityEngine;

namespace TPS.Editor.Tests
{
    public sealed class PhaseEnvironmentEditModeTests
    {
        [Test]
        public void GeneratedMarker_DefaultsToReplaceSafeEnvironmentSlot()
        {
            GameObject gameObject = new GameObject("EnvironmentMarkerTest");
            try
            {
                EnvironmentGeneratedMarker marker = gameObject.AddComponent<EnvironmentGeneratedMarker>();
                Assert.That(marker.GenerationId, Is.EqualTo("aster_harbor_environment"));
                Assert.That(marker.ReplaceSafe, Is.True);
                Assert.That(marker.PreserveManualChildren, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
