using UnityEngine;

namespace LaundryLegends.Core
{
    /// <summary>
    /// Verifies that persistent services initialized, then routes to the main menu.
    /// </summary>
    [RequireComponent(typeof(GameSession))]
    public sealed class BootstrapController : MonoBehaviour
    {
        private void Start()
        {
            GameSession session = GetComponent<GameSession>();
            if (session == null || session.State == null)
            {
                Debug.LogError("[Bootstrap] GameState failed to initialize; staying in Bootstrap scene.");
                return;
            }

            Debug.Log($"[Bootstrap] GameState ready. Money={session.State.Money}, Day={session.State.Day}.");
            SceneRouter.LoadMainMenu();
        }
    }
}
