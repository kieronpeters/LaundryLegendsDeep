using UnityEngine;

namespace LaundryLegends.Core
{
    /// <summary>
    /// Owns main-menu routing. Both buttons call these methods through serialized
    /// UnityEvent listeners, so the menu works with the mouse in a player build.
    /// There is deliberately no keyboard shortcut: an Enter handler would fire on
    /// top of the UI submit event and could route or quit twice in one press.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public void StartGame()
        {
            SceneRouter.LoadLaundromat();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
