using UnityEngine;

namespace LaundryLegends.Core
{
    /// <summary>
    /// Owns main-menu routing. UI buttons will call <see cref="StartGame"/> in a later phase.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartGame();
            }
        }

        public void StartGame()
        {
            SceneRouter.LoadLaundromat();
        }
    }
}
