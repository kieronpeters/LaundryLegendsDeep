using System.Collections.Generic;
using System.Text;
using LaundryLegends.Economy;
using UnityEngine;

namespace LaundryLegends.Customers
{
    /// <summary>
    /// Minimal IMGUI order ticket: shows every active order with its current
    /// stage and price. Text building is a pure function so it can be tested
    /// without rendering.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OrderTicketView : MonoBehaviour
    {
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField] private float _width = 260f;
        [SerializeField] private float _height = 280f;

        private string _receipt = string.Empty;
        private string _lastResult = string.Empty;
        private float _receiptUntil;

        public CustomerSpawner Spawner => _spawner;

        public void Configure(CustomerSpawner spawner)
        {
            _spawner = spawner;
        }

        /// <summary>Builds the ticket text for the ledger's active orders.</summary>
        public static string BuildText(OrderLedger ledger)
        {
            if (ledger == null || ledger.ActiveOrders.Count == 0)
            {
                return "No active order";
            }

            var builder = new StringBuilder();
            foreach (LaundryOrder order in ledger.ActiveOrders)
            {
                builder.Append("Order ").Append(order.OrderId)
                    .Append("  ").Append(StageWord(order))
                    .Append("  -  ").Append(order.Price).Append(" coins");
                builder.Append('\n');
            }

            return builder.ToString().TrimEnd('\n');
        }

        /// <summary>Wallet line for the HUD.</summary>
        public static string BuildWalletLine(OrderLedger ledger)
        {
            if (ledger == null || ledger.Wallet == null)
            {
                return "Coins: -";
            }

            return $"Coins: {ledger.Wallet.Money}";
        }

        /// <summary>One-line daily summary: income, costs, upgrades, net.</summary>
        public static string BuildSummaryText(EconomyService economy)
        {
            if (economy == null)
            {
                return "Today: -";
            }

            DailySummary summary = economy.GetSummary();
            return $"Today: +{summary.Income}  -{summary.Expenses}  -{summary.UpgradeSpend}  = {summary.Net}";
        }

        /// <summary>Upgrade list with owned markers.</summary>
        public static string BuildUpgradeText(EconomyService economy, IReadOnlyList<UpgradeDefinition> upgrades)
        {
            if (economy == null || upgrades == null)
            {
                return "Upgrades: -";
            }

            var builder = new StringBuilder();
            foreach (UpgradeDefinition upgrade in upgrades)
            {
                builder.Append(economy.HasUpgrade(upgrade.Id) ? "[x] " : "[ ] ")
                    .Append(upgrade.DisplayName)
                    .Append(" (")
                    .Append(upgrade.Cost)
                    .Append(")\n");
            }

            return builder.ToString().TrimEnd('\n');
        }

        /// <summary>Cheapest unowned upgrade, with the purchase key hint.</summary>
        public static string BuildNextGoalText(EconomyService economy, IReadOnlyList<UpgradeDefinition> upgrades)
        {
            UpgradeDefinition next = economy != null ? economy.FindNextGoal(upgrades) : null;
            if (next == null)
            {
                return "Next goal: none";
            }

            return $"Next goal: {next.DisplayName} ({next.Cost})   [U]";
        }

        private static string StageWord(LaundryOrder order)
        {
            switch (order.State)
            {
                case OrderState.Created:
                case OrderState.Accepted:
                    return "waiting for drop-off";
                case OrderState.Processing:
                    return "washing / drying";
                case OrderState.Ready:
                    return "ready to collect";
                case OrderState.Collected:
                    return "paying";
                case OrderState.Paid:
                    return "paid";
                default:
                    return "abandoned";
            }
        }

        private void Update()
        {
            UpdatePurchaseInput();

            if (_spawner == null)
            {
                return;
            }

            OrderLedger ledger = _spawner.Ledger;
            if (ledger == null || ledger.LastResult == _lastResult)
            {
                return;
            }

            _lastResult = ledger.LastResult;
            if (!string.IsNullOrEmpty(_lastResult))
            {
                _receipt = _lastResult;
                _receiptUntil = Time.time + 5f;
            }
        }

        private void OnGUI()
        {
            if (_spawner == null)
            {
                return;
            }

            OrderLedger ledger = _spawner.Ledger;
            if (ledger == null)
            {
                return;
            }

            EconomyService economy = _spawner.Economy;

            GUI.Box(new Rect(16f, 16f, _width, _height), "ORDER TICKET");
            GUI.Label(new Rect(28f, 42f, _width - 24f, 22f), BuildWalletLine(ledger));
            GUI.Label(new Rect(28f, 66f, _width - 24f, 60f), BuildText(ledger));
            GUI.Label(new Rect(28f, 126f, _width - 24f, 22f), BuildSummaryText(economy));
            GUI.Label(new Rect(28f, 148f, _width - 24f, 70f), BuildUpgradeText(economy, _spawner.Upgrades));
            GUI.Label(new Rect(28f, 222f, _width - 24f, 22f), BuildNextGoalText(economy, _spawner.Upgrades));

            if (Time.time < _receiptUntil && !string.IsNullOrEmpty(_receipt))
            {
                GUI.Label(new Rect(28f, 248f, _width - 24f, 24f), _receipt);
            }
        }

        private void UpdatePurchaseInput()
        {
            if (_spawner == null || !Input.GetKeyDown(KeyCode.U))
            {
                return;
            }

            _spawner.TryPurchaseNextUpgrade();
        }
    }
}
