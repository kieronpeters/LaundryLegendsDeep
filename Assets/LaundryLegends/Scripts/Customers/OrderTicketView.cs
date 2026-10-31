using System.Text;
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
        [SerializeField] private float _height = 140f;

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

            GUI.Box(new Rect(16f, 16f, _width, _height), "ORDER TICKET");
            GUI.Label(new Rect(28f, 42f, _width - 24f, 22f), BuildWalletLine(ledger));
            GUI.Label(new Rect(28f, 66f, _width - 24f, _height - 60f), BuildText(ledger));

            if (Time.time < _receiptUntil && !string.IsNullOrEmpty(_receipt))
            {
                GUI.Label(new Rect(28f, _height - 6f, _width - 24f, 24f), _receipt);
            }
        }
    }
}
