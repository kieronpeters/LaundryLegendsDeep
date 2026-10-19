using UnityEngine.SceneManagement;

namespace LaundryLegends.Core
{
    /// <summary>
    /// The single place that knows foundation scene names and transitions.
    /// </summary>
    public static class SceneRouter
    {
        public const string BootstrapScene = "Bootstrap";
        public const string MainMenuScene = "MainMenu";
        public const string LaundromatScene = "Laundromat";

        public static void LoadMainMenu()
        {
            SceneManager.LoadScene(MainMenuScene);
        }

        public static void LoadLaundromat()
        {
            SceneManager.LoadScene(LaundromatScene);
        }
    }
}
