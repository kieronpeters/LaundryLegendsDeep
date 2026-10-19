using System.Collections;
using LaundryLegends.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class SceneRoutingTests
    {
        [UnityTest]
        public IEnumerator BootstrapRoutesThroughMainMenuToLaundromat()
        {
            SceneManager.LoadScene(SceneRouter.BootstrapScene);
            yield return null;
            yield return null;

            Assert.AreEqual(SceneRouter.MainMenuScene, SceneManager.GetActiveScene().name,
                "Bootstrap should route to MainMenu.");

            GameSession session = Object.FindFirstObjectByType<GameSession>();
            Assert.IsNotNull(session, "GameSession should exist after Bootstrap.");
            Assert.IsNotNull(session.State, "GameState should be initialized.");
            Assert.AreEqual(100, session.State.Money, "Default starting money should be applied.");

            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "MainMenu should contain a MainMenuController.");

            menu.StartGame();
            yield return null;
            yield return null;

            Assert.AreEqual(SceneRouter.LaundromatScene, SceneManager.GetActiveScene().name,
                "MainMenu should route to Laundromat.");
            Assert.IsNotNull(Object.FindFirstObjectByType<GameSession>(),
                "GameSession should survive the scene transition.");
        }
    }
}
