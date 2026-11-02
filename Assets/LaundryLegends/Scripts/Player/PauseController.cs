using LaundryLegends.UI;
using UnityEngine;

namespace LaundryLegends.Player
{
    /// <summary>
    /// Minimal pause state: Esc releases the cursor and shows a placeholder overlay;
    /// Resume restores time and cursor lock. A real UI replaces the overlay later.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        public bool IsPaused { get; private set; }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            if (PurchaseMenuView.IsOpen || PurchaseMenuView.ClosedThisFrame)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            SetCursorLocked(false);
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = 1f;
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            if (IsPaused)
            {
                Resume();
            }
        }

        private void OnGUI()
        {
            if (!IsPaused)
            {
                return;
            }

            const float width = 280f;
            const float height = 130f;
            var rect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUI.Box(rect, "Paused");
            if (GUI.Button(new Rect(rect.x + 90f, rect.y + 70f, 100f, 30f), "Resume"))
            {
                Resume();
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            if (Application.isBatchMode)
            {
                return;
            }

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
