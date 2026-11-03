using System;
using LaundryLegends.Audio;
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
            if (PurchaseMenuView.IsOpen || PurchaseMenuView.ClosedThisFrame
                || BillsPanelView.IsOpen || BillsPanelView.ClosedThisFrame)
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

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            const float width = 380f;
            const float height = 300f;
            var rect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUI.color = new Color(0.03f, 0.04f, 0.06f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;

            GUI.Box(rect, "Paused");
            GUI.Label(new Rect(rect.x + 20f, rect.y + 34f, width - 40f, 22f), "Audio");

            GameAudioSettings.SetMaster(Slider(rect.x + 20f, rect.y + 64f, "Master", GameAudioSettings.Master));
            GameAudioSettings.SetMusic(Slider(rect.x + 20f, rect.y + 100f, "Music", GameAudioSettings.Music));
            GameAudioSettings.SetSfx(Slider(rect.x + 20f, rect.y + 136f, "Effects", GameAudioSettings.Sfx));

            if (GUI.Button(new Rect(rect.x + 20f, rect.y + height - 52f, 120f, 32f), "Reset audio"))
            {
                GameAudioSettings.ResetToDefaults();
            }

            if (GUI.Button(new Rect(rect.x + width - 140f, rect.y + height - 52f, 120f, 32f), "Resume"))
            {
                Resume();
            }
        }

        private static float Slider(float x, float y, string label, float value)
        {
            GUI.Label(new Rect(x, y, 90f, 22f), label);
            float next = GUI.HorizontalSlider(new Rect(x + 95f, y + 8f, 180f, 20f), value, 0f, 1f);
            GUI.Label(new Rect(x + 285f, y, 50f, 22f), $"{Mathf.RoundToInt(next * 100f)}%");
            return next;
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
