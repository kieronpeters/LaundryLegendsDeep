using System;
using System.Collections.Generic;
using System.Globalization;
using LaundryLegends.Core;
using LaundryLegends.Laundry;

namespace LaundryLegends.Customers
{
    /// <summary>
    /// Owns every <see cref="LaundryOrder"/> for one session: registration,
    /// lookup by id or item, lifecycle transitions, and payment.
    /// Payment is guarded here, in the model, not in the UI: an order id can be
    /// credited at most once for the lifetime of this ledger.
    /// </summary>
    public sealed class OrderLedger
    {
        private readonly Dictionary<string, LaundryOrder> _ordersById =
            new Dictionary<string, LaundryOrder>(StringComparer.Ordinal);

        private readonly List<string> _registrationOrder = new List<string>();

        private readonly HashSet<string> _settledOrderIds = new HashSet<string>(StringComparer.Ordinal);

        private readonly GameState _wallet;

        private int _nextOrderNumber;

        /// <summary>
        /// Creates a ledger that credits <paramref name="wallet"/>. The wallet is
        /// required so an order can never reach <see cref="OrderState.Paid"/>
        /// without the money moving.
        /// </summary>
        /// <exception cref="ArgumentNullException">The wallet is null.</exception>
        public OrderLedger(GameState wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        /// <summary>The single money sink every payment goes through.</summary>
        public GameState Wallet => _wallet;

        /// <summary>Number of registered orders in every state.</summary>
        public int Count => _ordersById.Count;

        /// <summary>Human-readable outcome of the most recent call, for prompts and receipts.</summary>
        public string LastResult { get; private set; } = string.Empty;

        /// <summary>Every registered order, in registration order. Allocates a new list per access.</summary>
        public IReadOnlyList<LaundryOrder> AllOrders => BuildList(null);

        /// <summary>Orders that still need work or payment, in registration order.</summary>
        public IReadOnlyList<LaundryOrder> ActiveOrders => BuildList(order => order.IsActive);

        /// <summary>Paid or abandoned orders, in registration order.</summary>
        public IReadOnlyList<LaundryOrder> CompletedOrders => BuildList(order => order.IsTerminal);

        /// <summary>
        /// Registers an externally created order. Rejects a null order, a duplicate
        /// id, and a second active order for the same item instance.
        /// </summary>
        public bool TryRegisterOrder(LaundryOrder order)
        {
            if (order == null)
            {
                return Fail("Cannot register a null order.");
            }

            if (_ordersById.ContainsKey(order.OrderId))
            {
                return Fail($"Order id '{order.OrderId}' already exists.");
            }

            if (TryFindOrderForItem(order.Item, out LaundryOrder existing) && existing.IsActive)
            {
                return Fail($"Item already belongs to active order {existing.OrderId}.");
            }

            _ordersById.Add(order.OrderId, order);
            _registrationOrder.Add(order.OrderId);
            LastResult = $"Order {order.OrderId} registered.";
            return true;
        }

        /// <summary>
        /// Creates and registers a ticket with a generated, never-reused id such
        /// as <c>ORD-0001</c>. Returns false for a null item, a negative price, a
        /// non-finite time, or an item that already has an active order.
        /// </summary>
        public bool TryCreateOrder(LaundryItem item, int price, float createdAtSeconds, out LaundryOrder order)
        {
            order = null;

            if (item == null)
            {
                return Fail("Cannot create an order without a laundry item.");
            }

            if (price < 0)
            {
                return Fail("Order price cannot be negative.");
            }

            if (float.IsNaN(createdAtSeconds) || float.IsInfinity(createdAtSeconds))
            {
                return Fail("Order creation time must be finite.");
            }

            string orderId;
            do
            {
                _nextOrderNumber += 1;
                orderId = "ORD-" + _nextOrderNumber.ToString("D4", CultureInfo.InvariantCulture);
            }
            while (_ordersById.ContainsKey(orderId));

            order = new LaundryOrder(orderId, item, price, createdAtSeconds);
            if (!TryRegisterOrder(order))
            {
                order = null;
                return false;
            }

            LastResult = $"Order {orderId} created at {price}.";
            return true;
        }

        /// <summary>Looks an order up by id. Null, empty, unknown, or whitespace ids return false without throwing.</summary>
        public bool TryGetOrder(string orderId, out LaundryOrder order)
        {
            order = null;
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            return _ordersById.TryGetValue(orderId, out order);
        }

        /// <summary>
        /// Finds the most recently registered order for <paramref name="item"/>.
        /// Identity is the item reference, so a shared template id cannot match.
        /// </summary>
        public bool TryFindOrderForItem(LaundryItem item, out LaundryOrder order)
        {
            order = null;
            if (ReferenceEquals(item, null))
            {
                return false;
            }

            for (int i = _registrationOrder.Count - 1; i >= 0; i--)
            {
                LaundryOrder candidate = _ordersById[_registrationOrder[i]];
                if (!ReferenceEquals(candidate.Item, item))
                {
                    continue;
                }

                order = candidate;
                return true;
            }

            return false;
        }

        /// <summary>Convenience wrapper over <see cref="TryFindOrderForItem"/>; returns null when no order exists.</summary>
        public LaundryOrder FindOrderForItem(LaundryItem item)
        {
            return TryFindOrderForItem(item, out LaundryOrder order) ? order : null;
        }

        /// <summary>Moves a <see cref="OrderState.Created"/> order to <see cref="OrderState.Accepted"/>.</summary>
        public bool TryAcceptOrder(string orderId, float atSeconds)
        {
            return Advance(orderId, OrderState.Accepted, atSeconds, "accepted");
        }

        /// <summary>Moves an accepted order into <see cref="OrderState.Processing"/>.</summary>
        public bool TryMarkProcessing(string orderId, float atSeconds)
        {
            return Advance(orderId, OrderState.Processing, atSeconds, "processing");
        }

        /// <summary>Moves a processing order to <see cref="OrderState.Ready"/>. Accepted orders must pass through processing first.</summary>
        public bool TryMarkReady(string orderId, float atSeconds)
        {
            return Advance(orderId, OrderState.Ready, atSeconds, "ready");
        }

        /// <summary>Moves a ready order to <see cref="OrderState.Collected"/>. Collecting before ready is rejected.</summary>
        public bool TryCollect(string orderId, float atSeconds)
        {
            return Advance(orderId, OrderState.Collected, atSeconds, "collected");
        }

        /// <summary>Abandons any order that has not already settled, leaving it registered and immutable.</summary>
        public bool TryAbandon(string orderId, float atSeconds)
        {
            return Advance(orderId, OrderState.Abandoned, atSeconds, "abandoned");
        }

        /// <summary>
        /// Pays a collected order exactly once: transitions it to
        /// <see cref="OrderState.Paid"/> and credits <see cref="LaundryOrder.Price"/>
        /// to the wallet. A second call for the same id returns false and credits
        /// nothing; an order that is not collected is rejected before any money moves.
        /// </summary>
        public bool TryPay(string orderId, float atSeconds)
        {
            if (!TryGetOrder(orderId, out LaundryOrder order))
            {
                return FailUnknownOrder(orderId);
            }

            if (_settledOrderIds.Contains(order.OrderId))
            {
                return Fail($"Order {order.OrderId} was already paid; no money was credited.");
            }

            if (!order.CanTransitionTo(OrderState.Paid))
            {
                return Fail($"Order {order.OrderId} cannot be paid while {order.State}.");
            }

            if (!_wallet.CanCredit(order.Price))
            {
                return Fail($"Wallet cannot credit {order.Price} for order {order.OrderId}.");
            }

            if (!order.TryTransition(OrderState.Paid, atSeconds))
            {
                return Fail(order.LastTransitionError);
            }

            _settledOrderIds.Add(order.OrderId);
            _wallet.TryCredit(order.Price); // Guaranteed by the CanCredit check above.

            LastResult = order.Price > 0
                ? $"Paid {order.Price} for order {order.OrderId}."
                : $"Order {order.OrderId} settled with no charge.";
            return true;
        }

        private bool Advance(string orderId, OrderState next, float atSeconds, string resultWord)
        {
            if (!TryGetOrder(orderId, out LaundryOrder order))
            {
                return FailUnknownOrder(orderId);
            }

            if (!order.TryTransition(next, atSeconds))
            {
                return Fail(order.LastTransitionError);
            }

            LastResult = $"Order {order.OrderId} {resultWord}.";
            return true;
        }

        private IReadOnlyList<LaundryOrder> BuildList(Func<LaundryOrder, bool> predicate)
        {
            var result = new List<LaundryOrder>(_registrationOrder.Count);
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                LaundryOrder order = _ordersById[_registrationOrder[i]];
                if (predicate == null || predicate(order))
                {
                    result.Add(order);
                }
            }

            return result;
        }

        private bool FailUnknownOrder(string orderId)
        {
            return Fail($"Unknown order id '{orderId}'.");
        }

        private bool Fail(string message)
        {
            LastResult = message;
            return false;
        }
    }
}
