using LaundryLegends.Core;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Guards the shipped menu. A button whose click handler was registered with
    /// <c>onClick.AddListener</c> at edit time looks correct in the editor but has an
    /// empty, non-serialized click list in a player build, so it silently does nothing.
    /// These tests assert the handlers are persistent and reachable by the mouse.
    /// </summary>
    public sealed class MainMenuButtonTests
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/MainMenu.unity";

        private Scene scene;
        private bool openedHere;

        [SetUp]
        public void OpenMenuScene()
        {
            scene = SceneManager.GetSceneByName(SceneRouter.MainMenuScene);
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }
        }

        [TearDown]
        public void CloseMenuScene()
        {
            if (openedHere && scene.IsValid())
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private Button FindButton(string name)
        {
            foreach (Button button in InScene<Button>())
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail($"MainMenu should contain a '{name}' button.");
            return null;
        }

        private System.Collections.Generic.List<T> InScene<T>() where T : Component
        {
            var found = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found;
        }

        private static void AssertSerializedHandler(Button button, string methodName)
        {
            Assert.AreEqual(
                1,
                button.onClick.GetPersistentEventCount(),
                $"{button.name} must have exactly one serialized click handler or it does nothing in a build.");

            Assert.AreEqual(
                methodName,
                button.onClick.GetPersistentMethodName(0),
                $"{button.name} should call {methodName}.");

            Assert.IsInstanceOf<MainMenuController>(
                button.onClick.GetPersistentTarget(0),
                $"{button.name} should target MainMenuController.");

            Assert.AreNotEqual(
                UnityEventCallState.Off,
                button.onClick.GetPersistentListenerState(0),
                $"{button.name}'s handler is disabled and would never fire.");
        }

        [Test]
        public void StartButtonHasSerializedStartHandler()
        {
            AssertSerializedHandler(FindButton("PlayButton"), "StartGame");
        }

        [Test]
        public void ExitButtonHasSerializedQuitHandler()
        {
            AssertSerializedHandler(FindButton("QuitButton"), "QuitGame");
        }

        [Test]
        public void MenuCanReceiveMouseClicks()
        {
            Assert.IsNotEmpty(
                InScene<GraphicRaycaster>(),
                "The menu canvas needs a GraphicRaycaster or no click is ever raycast.");

            var eventSystems = InScene<EventSystem>();
            Assert.AreEqual(1, eventSystems.Count, "The menu needs exactly one EventSystem.");

            foreach (EventSystem eventSystem in eventSystems)
            {
                Assert.IsNotNull(
                    eventSystem.GetComponent<BaseInputModule>(),
                    "The EventSystem needs an input module to deliver pointer events.");
            }
        }

        [Test]
        public void MenuHasExactlyOneController()
        {
            var controllers = InScene<MainMenuController>();
            Assert.AreEqual(
                1,
                controllers.Count,
                "A duplicate controller would handle every press twice.");

            Button start = FindButton("PlayButton");
            Assert.AreSame(
                controllers[0],
                start.onClick.GetPersistentTarget(0),
                "The serialized handler must point at the one controller in the scene.");
        }

        [Test]
        public void MenuHasAnAudioListenerAndMusic()
        {
            Assert.AreEqual(1, InScene<AudioListener>().Count, "The menu music needs exactly one listener.");

            var sources = InScene<AudioSource>();
            Assert.IsNotEmpty(sources, "The menu should have a music AudioSource.");

            bool hasClip = false;
            foreach (AudioSource source in sources)
            {
                hasClip |= source.clip != null;
            }

            Assert.IsTrue(hasClip, "The menu music source should have a clip assigned.");
        }

        [Test]
        public void StartButtonIsLargeEnoughToClick()
        {
            Button button = FindButton("PlayButton");
            Rect rect = button.GetComponent<RectTransform>().rect;
            Assert.Greater(rect.width, 200f, "START GAME should be comfortably clickable.");
            Assert.Greater(rect.height, 60f, "START GAME should be comfortably clickable.");
        }
    }
}
