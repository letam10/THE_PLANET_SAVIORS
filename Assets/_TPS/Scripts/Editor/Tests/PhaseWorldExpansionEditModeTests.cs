using System.IO;
using NUnit.Framework;

namespace TPS.Editor.Tests
{
    public sealed class PhaseWorldExpansionEditModeTests
    {
        [Test]
        public void ExpandedWorldScenes_ExistOnDisk()
        {
            Assert.That(File.Exists("Assets/_TPS/Scenes/World/ZN_Settlement_Gullwatch.unity"), Is.True);
            Assert.That(File.Exists("Assets/_TPS/Scenes/World/ZN_Settlement_RedCedar.unity"), Is.True);
            Assert.That(File.Exists("Assets/_TPS/Scenes/Dungeons/DG_TideCaverns.unity"), Is.True);
            Assert.That(File.Exists("Assets/_TPS/Scenes/Dungeons/DG_QuarryRuins.unity"), Is.True);
        }

        [Test]
        public void WindowsBuildProfile_ContainsExpandedWorldScenes()
        {
            string content = File.ReadAllText("Assets/Settings/Build Profiles/Windows.asset");

            Assert.That(content.Contains("Assets/_TPS/Scenes/World/ZN_Settlement_Gullwatch.unity"), Is.True);
            Assert.That(content.Contains("Assets/_TPS/Scenes/World/ZN_Settlement_RedCedar.unity"), Is.True);
            Assert.That(content.Contains("Assets/_TPS/Scenes/Dungeons/DG_TideCaverns.unity"), Is.True);
            Assert.That(content.Contains("Assets/_TPS/Scenes/Dungeons/DG_QuarryRuins.unity"), Is.True);
        }
    }
}
