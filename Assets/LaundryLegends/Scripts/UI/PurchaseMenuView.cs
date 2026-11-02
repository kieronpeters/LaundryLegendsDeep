using System;
using System.Collections.Generic;
using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.UI
{
    /// <summary>
    /// One list for every purchase: laundry upgrades, arcade machines, arcade
    /// upgrades and the expansion. Opens with U, pauses the shop, and replaces the
    /// old ground pedestals. Locked entries explain why they cannot be bought.
    /// </summary>
    public sealed class PurchaseMenuView : MonoBehaviour
    {
        private sealed class Entry
        {
            public string Label;
            public string Status;
            public bool Enabled;
            public Action Buy;
        }

        [SerializeField] private ArcadeService _arcade;
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField] private PauseController _pause;
        [SerializeField] private float _width = 760f;
        [SerializeField] private float _rowHeight = 30f;

        private readonly List<Entry> _entries = new List<Entry>();
        private int _selected;

        public static bool IsOpen { get; private set; }

        /// <summary>True for the rest of the frame in which the menu closed.</summary>
        public static bool ClosedThisFrame { get; private set; }

        public bool IsVisible => IsOpen;

        public IReadOnlyList<string> EntryLabels
        {
            get
            {
                var labels = new List<string>(_entries.Count);
                foreach (Entry entry in _entries)
                {
                    labels.Add($"{entry.Label} [{entry.Status}]");
                }

                return labels;
            }
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

            if (BillsPanelView.IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                if (IsOpen)
                {
                    Close();
                }
                else if (Time.timeScale > 0f && (_pause == null || !_pause.IsPaused))
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

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                Move(-1);
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                Move(1);
            }

            if (Input.GetKeyDown(KeyCode.E)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                BuySelected();
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            Resolve();
            Rebuild();
            _selected = 0;
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

        /// <summary>Rebuilds the entry list; public for tests and after a purchase.</summary>
        public void Rebuild()
        {
            _entries.Clear();
            bool arcadeReady = _arcade != null && _arcade.IsRoomReady;

            if (_spawner != null)
            {
                foreach (UpgradeDefinition upgrade in _spawner.Upgrades)
                {
                    if (upgrade == null)
                    {
                        continue;
                    }

                    bool owned = _spawner.Economy.HasUpgrade(upgrade.Id);
                    _entries.Add(new Entry
                    {
                        Label = $"Laundry: {upgrade.DisplayName}",
                        Status = owned ? "owned" : $"${upgrade.Cost}",
                        Enabled = !owned,
                        Buy = () => _spawner.TryPurchaseUpgrade(upgrade)
                    });
                }
            }

            if (_arcade != null)
            {
                var machineNameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int slot = 1; slot < _arcade.SlotCount; slot++)
                {
                    ArcadeMachineDefinition definition = _arcade.GetSlotDefinition(slot);
                    if (definition != null)
                    {
                        machineNameCounts.TryGetValue(definition.DisplayName, out int seenCount);
                        machineNameCounts[definition.DisplayName] = seenCount + 1;
                    }
                }

                for (int slot = 1; slot < _arcade.SlotCount; slot++)
                {
                    ArcadeMachineDefinition definition = _arcade.GetSlotDefinition(slot);
                    if (definition == null)
                    {
                        continue;
                    }

                    bool installed = _arcade.IsSlotInstalled(slot);
                    bool reachable = _arcade.IsSlotReachable(slot);
                    bool enabled = arcadeReady && !installed && reachable;
                    string status;
                    if (installed)
                    {
                        status = "installed";
                    }
                    else if (!arcadeReady)
                    {
                        status = "room locked";
                    }
                    else if (!reachable)
                    {
                        status = "needs expansion";
                    }
                    else
                    {
                        status = $"${_arcade.GetSlotCost(slot)}";
                    }

                    // Bay anchors reuse the shop definitions, so identical names
                    // would otherwise read as duplicate rows.
                    bool duplicated = machineNameCounts.TryGetValue(definition.DisplayName, out int count) && count > 1;
                    string label = duplicated
                        ? $"Machine: {definition.DisplayName} (#{slot})"
                        : $"Machine: {definition.DisplayName}";

                    int capturedSlot = slot;
                    _entries.Add(new Entry
                    {
                        Label = label,
                        Status = status,
                        Enabled = enabled,
                        Buy = () => _arcade.TryInstall(capturedSlot, Time.time)
                    });
                }

                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (ArcadeUpgradePoint point in FindObjectsByType<ArcadeUpgradePoint>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    ArcadeUpgradeDefinition upgrade = point != null ? point.Upgrade : null;
                    if (upgrade == null || !seen.Add(upgrade.Id))
                    {
                        continue;
                    }

                    bool owned = _arcade.HasUpgrade(upgrade.Id);
                    bool gated = upgrade.RequiresExpansion && !_arcade.IsExpanded;
                    bool enabled = arcadeReady && !owned && !gated;
                    string status;
                    if (owned)
                    {
                        status = "owned";
                    }
                    else if (!arcadeReady)
                    {
                        status = "room locked";
                    }
                    else if (gated)
                    {
                        status = "needs expansion";
                    }
                    else
                    {
                        status = $"${upgrade.Cost}";
                    }

                    ArcadeUpgradeDefinition captured = upgrade;
                    _entries.Add(new Entry
                    {
                        Label = $"Arcade: {upgrade.DisplayName}",
                        Status = status,
                        Enabled = enabled,
                        Buy = () => _arcade.TryBuyUpgrade(captured, Time.time)
                    });
                }

                bool expanded = _arcade.IsExpanded;
                _entries.Add(new Entry
                {
                    Label = "Arcade expansion bay",
                    Status = expanded ? "open" : (arcadeReady ? $"${_arcade.ExpansionCost}" : "room locked"),
                    Enabled = arcadeReady && !expanded,
                    Buy = () => _arcade.TryExpand(Time.time)
                });
            }

            if (_selected >= _entries.Count)
            {
                _selected = Mathf.Max(0, _entries.Count - 1);
            }
        }

        /// <summary>Readable summary for tests.</summary>
        public string BuildText()
        {
            Rebuild();
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("PURCHASES");
            foreach (string label in EntryLabels)
            {
                builder.AppendLine(label);
            }

            if (_arcade != null && !string.IsNullOrEmpty(_arcade.LastResult))
            {
                builder.AppendLine(_arcade.LastResult);
            }

            return builder.ToString();
        }

        private void Move(int delta)
        {
            if (_entries.Count == 0)
            {
                return;
            }

            _selected = (_selected + delta + _entries.Count) % _entries.Count;
        }

        private void BuySelected()
        {
            if (_selected < 0 || _selected >= _entries.Count)
            {
                return;
            }

            Buy(_entries[_selected]);
        }

        private void Buy(Entry entry)
        {
            if (entry == null || !entry.Enabled || entry.Buy == null)
            {
                return;
            }

            entry.Buy();
            Rebuild();
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            float height = 92f + Mathf.Max(1, _entries.Count) * _rowHeight;
            var rect = new Rect(
                (Screen.width - _width) * 0.5f,
                Mathf.Max(24f, (Screen.height - height) * 0.5f),
                _width,
                height);

            // Solid backdrop so the list stays readable over the bright shop.
            Color previousColor = GUI.color;
            GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.Box(rect, "PURCHASES");
            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 26f, rect.width - 32f, 22f),
                "W/S select   E buy   U/Esc close");

            float y = rect.y + 56f;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                var row = new Rect(rect.x + 16f, y, rect.width - 32f, _rowHeight - 4f);
                GUI.color = i == _selected
                    ? new Color(0.2f, 0.45f, 0.9f, 0.75f)
                    : new Color(0.08f, 0.09f, 0.12f, 0.85f);
                GUI.DrawTexture(row, Texture2D.whiteTexture);
                GUI.color = previousColor;

                string marker = i == _selected ? "> " : "  ";
                string text = $"{marker}{entry.Label}   [{entry.Status}]";
                if (entry.Enabled)
                {
                    if (GUI.Button(row, text))
                    {
                        Buy(entry);
                    }
                }
                else
                {
                    GUI.Label(row, text);
                }

                y += _rowHeight;
            }

            if (_arcade != null && !string.IsNullOrEmpty(_arcade.LastResult))
            {
                GUI.Label(
                    new Rect(rect.x + 16f, rect.y + height - 26f, rect.width - 32f, 22f),
                    _arcade.LastResult);
            }
        }

        private void Resolve()
        {
            if (_arcade == null)
            {
                _arcade = FindFirstObjectByType<ArcadeService>();
            }

            if (_spawner == null)
            {
                _spawner = FindFirstObjectByType<CustomerSpawner>();
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
