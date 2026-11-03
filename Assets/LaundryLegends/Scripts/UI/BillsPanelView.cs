using System.Text;
using LaundryLegends.Arcade;
using LaundryLegends.Business;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.UI
{
    /// <summary>
    /// B toggles the bills menu. It lists the unpaid rent/electricity/water bill,
    /// pauses the shop, and pays it with P or the button. An unaffordable bill stays
    /// open; the wallet is never driven negative.
    /// </summary>
    public sealed class BillsPanelView : MonoBehaviour
    {
        [SerializeField] private BillingService _billing;
        [SerializeField] private PauseController _pause;
        [SerializeField] private float _width = 620f;
        [SerializeField] private float _height = 260f;

        public static bool IsOpen { get; private set; }
        public static bool ClosedThisFrame { get; private set; }

        public BillingService Billing => _billing;

        public void Configure(BillingService billing, PauseController pause = null)
        {
            _billing = billing;
            _pause = pause;
        }

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Resolve();
        }

        private void OnDisable()
        {
            if (IsOpen)
            {
                Close();
            }
        }

        private void LateUpdate()
        {
            ClosedThisFrame = false;
        }

        private void Update()
        {
            Resolve();
            if (ArcadeMode.IsActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (IsOpen)
                {
                    Close();
                }
                else if (Time.timeScale > 0f
                         && (_pause == null || !_pause.IsPaused)
                         && !PurchaseMenuView.IsOpen)
                {
                    Open();
                }

                return;
            }

            if (!IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.P)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                PayCurrent();
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            Resolve();
            IsOpen = true;
            Time.timeScale = 0f;
            SetCursorLocked(false);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            ClosedThisFrame = true;
            Time.timeScale = 1f;
            SetCursorLocked(true);
        }

        public bool PayCurrent()
        {
            if (_billing == null)
            {
                return false;
            }

            return _billing.TryPayCurrentFromSpawner(Time.time);
        }

        public string BuildText()
        {
            if (_billing == null)
            {
                return "BILLS\n(not wired)";
            }

            UtilityBill bill = _billing.CurrentBill;
            if (bill == null)
            {
                return "BILLS\nNo unpaid bill.\n\nB/Esc close";
            }

            var builder = new StringBuilder();
            builder.AppendLine($"BILLS  due day {bill.DueDay}");
            foreach (BillLine line in bill.Lines)
            {
                builder.AppendLine($"  {line.Label}: ${line.Amount}");
            }

            builder.AppendLine($"  TOTAL: ${bill.Total}");
            builder.AppendLine("P pay   B/Esc close");
            if (!string.IsNullOrEmpty(_billing.LastResult))
            {
                builder.AppendLine(_billing.LastResult);
            }

            return builder.ToString();
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            var rect = new Rect(
                (Screen.width - _width) * 0.5f,
                (Screen.height - _height) * 0.5f,
                _width,
                _height);

            Color previous = GUI.color;
            GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;

            GUI.Box(rect, "BILLS");
            GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = false };
            GUI.Label(new Rect(rect.x + 24f, rect.y + 36f, rect.width - 48f, rect.height - 100f), BuildText(), body);

            if (_billing != null && _billing.CurrentBill != null)
            {
                if (GUI.Button(new Rect(rect.x + 24f, rect.y + rect.height - 56f, 160f, 34f), "Pay bill"))
                {
                    PayCurrent();
                }
            }

            if (GUI.Button(new Rect(rect.x + rect.width - 136f, rect.y + rect.height - 56f, 112f, 34f), "Close"))
            {
                Close();
            }
        }

        private void Resolve()
        {
            if (_billing == null)
            {
                _billing = FindFirstObjectByType<BillingService>();
            }

            if (_pause == null)
            {
                _pause = FindFirstObjectByType<PauseController>();
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
