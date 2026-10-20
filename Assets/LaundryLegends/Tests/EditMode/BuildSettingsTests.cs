using System.IO;
using System.Linq;
using LaundryLegends.Core;
using NUnit.Framework;
using UnityEditor;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class BuildSettingsTests
    {
        [Test]
        public void FoundationScenesAreInExpectedBuildOrder()
        {
            string[] expected =
            {
                SceneRouter.BootstrapScene,
                SceneRouter.MainMenuScene,
                SceneRouter.LaundromatScene
            };

            string[] actual = EditorBuildSettings.scenes
                .Take(expected.Length)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void FoundationScenesAreEnabled()
        {
            string[] expected =
            {
                SceneRouter.BootstrapScene,
                SceneRouter.MainMenuScene,
                SceneRouter.LaundromatScene
            };

            foreach (string sceneName in expected)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes
                    .FirstOrDefault(candidate => Path.GetFileNameWithoutExtension(candidate.path) == sceneName);

                Assert.IsNotNull(scene, $"Build settings should contain {sceneName}.");
                Assert.IsTrue(scene.enabled, $"{sceneName} should be enabled in build settings.");
            }
        }
    }
}
