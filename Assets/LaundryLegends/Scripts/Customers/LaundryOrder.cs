using System;
using System.Collections.Generic;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.Customers
{
    /// <summary>
    /// Lifecycle of a single customer order. The order records business state
    /// only; the physical laundering stage stays on the <see cref="LaundryItem"/>.
    /// </summary>
    public enum OrderState
    {
        /// <summary>Ticket exists but no staff member has taken it yet.</summary>
        Created = 0,

        /// <summary>Staff accepted the ticket; the item is still dirty.</summary>
        Accepted = 1,

        /// <summary>The item is inside the wash/dry/fold/wrap chain.</summary>
        Processing = 2,

        /// <summary>The wrapped bundle is waiting for the customer or a courier.</summary>
        Ready = 3,

        /// <summary>The customer has the item; payment is still owed.</summary>
        Collected = 4,

        /// <summary>Payment was credited exactly once. Terminal.</summary>
        Paid = 5,

        /// <summary>The customer or staff gave up on the order. Terminal.</summary>
        Abandoned = 6
    }

    /// <summary>
    /// One customer order: a stable id, the item instance it refers to, a price
    /// fixed at creation, its lifecycle state, and the time of every transition.
    /// The model is plain C# with no scene lookups, no clock reads, and no
    /// randomness, so the same inputs always produce the same state.
    /// </summary>
    public sealed class LaundryOrder
    {
        /// <summary>
        /// The single source of truth for legal transitions. Every mutator on
        /// this type and on <see cref="OrderLedger"/> goes through it.
        /// </summary>
        private static readonly (OrderState From, OrderState To)[] Rules =
        {
            (OrderState.Created, OrderState.Accepted),
            (OrderState.Created, OrderState.Abandoned),
            (OrderState.Accepted, OrderState.Processing),
            (OrderState.Accepted, OrderState.Abandoned),
            (OrderState.Processing, OrderState.Ready),
            (OrderState.Processing, OrderState.Abandoned),
            (OrderState.Ready, OrderState.Collected),
            (OrderState.Ready, OrderState.Abandoned),
            (OrderState.Collected, OrderState.Paid)
        };

        private static readonly IReadOnlyList<(OrderState From, OrderState To)> RuleList =
            Array.AsReadOnly(Rules);

        private readonly LaundryItem _item;
        private LaundryStage _recordedStage;

        /// <summary>
        /// Creates an order in <see cref="OrderState.Created"/>. Ids are supplied
        /// by the caller so <see cref="OrderLedger"/> can guarantee uniqueness.
        /// </summary>
        /// <exception cref="ArgumentException">The id is null, empty, or whitespace.</exception>
        /// <exception cref="ArgumentNullException">The item reference is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The price is negative or the time is not finite.</exception>
        public LaundryOrder(string orderId, LaundryItem item, int price, float createdAtSeconds)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException("Order id cannot be empty.", nameof(orderId));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "An order must reference a laundry item.");
            }

            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), "Order price cannot be negative.");
            }

            if (!IsFinite(createdAtSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(createdAtSeconds), "Order time must be finite.");
            }

            OrderId = orderId;
            _item = item;
            ItemId = item.InstanceId;
            Price = price;
            CreatedAtSeconds = createdAtSeconds;
            StateChangedAtSeconds = createdAtSeconds;
            State = OrderState.Created;
            _recordedStage = item.Stage;
        }

        /// <summary>Stable identifier; unique inside one <see cref="OrderLedger"/>.</summary>
        public string OrderId { get; }

        /// <summary>The item instance this order is for. May be destroyed later; identity never changes.</summary>
        public LaundryItem Item => _item;

        /// <summary>The item's instance id captured at creation, valid even after the object is gone.</summary>
        public string ItemId { get; }

        /// <summary>Agreed price, fixed when the order was created. There is no setter.</summary>
        public int Price { get; }

        /// <summary>Current lifecycle state.</summary>
        public OrderState State { get; private set; }

        /// <summary>Seconds recorded when the order was created.</summary>
        public float CreatedAtSeconds { get; }

        /// <summary>Seconds recorded when the order was accepted, or null while still <see cref="OrderState.Created"/>.</summary>
        public float? AcceptedAtSeconds { get; private set; }

        /// <summary>Seconds recorded when processing started, or null.</summary>
        public float? ProcessingAtSeconds { get; private set; }

        /// <summary>Seconds recorded when the order became ready, or null.</summary>
        public float? ReadyAtSeconds { get; private set; }

        /// <summary>Seconds recorded when the customer collected the item, or null.</summary>
        public float? CollectedAtSeconds { get; private set; }

        /// <summary>Seconds recorded for the single successful payment, or null.</summary>
        public float? PaidAtSeconds { get; private set; }

        /// <summary>Seconds recorded when the order was abandoned, or null.</summary>
        public float? AbandonedAtSeconds { get; private set; }

        /// <summary>Seconds of the most recent successful transition; starts at creation time.</summary>
        public float StateChangedAtSeconds { get; private set; }

        /// <summary>True once the order reached <see cref="OrderState.Paid"/> or <see cref="OrderState.Abandoned"/>.</summary>
        public bool IsTerminal => State == OrderState.Paid || State == OrderState.Abandoned;

        /// <summary>True while the order still needs work or payment.</summary>
        public bool IsActive => !IsTerminal;

        /// <summary>
        /// Mirror of the item's physical stage for ticket display. Falls back to
        /// the last captured value when the item object no longer exists.
        /// </summary>
        public LaundryStage Stage => _item != null ? _item.Stage : _recordedStage;

        /// <summary>Reason the most recent <see cref="TryTransition"/> was rejected; empty after a success.</summary>
        public string LastTransitionError { get; private set; } = string.Empty;

        /// <summary>Read-only view of every legal (from, to) pair. Tests iterate this.</summary>
        public static IReadOnlyList<(OrderState From, OrderState To)> TransitionRules => RuleList;

        /// <summary>True when the transition table allows <paramref name="from"/> to become <paramref name="to"/>.</summary>
        public static bool IsTransitionAllowed(OrderState from, OrderState to)
        {
            for (int i = 0; i < Rules.Length; i++)
            {
                if (Rules[i].From == from && Rules[i].To == to)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when this order may legally move to <paramref name="next"/> right now.</summary>
        public bool CanTransitionTo(OrderState next)
        {
            return IsTransitionAllowed(State, next);
        }

        /// <summary>
        /// Applies one transition. An illegal transition, a non-finite time, or a
        /// move to the current state returns false and leaves state, timestamps,
        /// and <see cref="StateChangedAtSeconds"/> untouched.
        /// </summary>
        public bool TryTransition(OrderState next, float atSeconds)
        {
            if (!IsTransitionAllowed(State, next))
            {
                LastTransitionError = $"Order {OrderId} cannot go from {State} to {next}.";
                return false;
            }

            if (!IsFinite(atSeconds))
            {
                LastTransitionError = "Transition time must be finite.";
                return false;
            }

            State = next;
            StateChangedAtSeconds = atSeconds;
            RecordTimestamp(next, atSeconds);
            LastTransitionError = string.Empty;
            return true;
        }

        /// <summary>Refreshes the displayed <see cref="Stage"/> from the live item, when it still exists.</summary>
        public void CaptureStage()
        {
            if (_item != null)
            {
                _recordedStage = _item.Stage;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Order {OrderId} [{State}] price {Price}";
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void RecordTimestamp(OrderState state, float atSeconds)
        {
            switch (state)
            {
                case OrderState.Accepted:
                    AcceptedAtSeconds = atSeconds;
                    break;
                case OrderState.Processing:
                    ProcessingAtSeconds = atSeconds;
                    break;
                case OrderState.Ready:
                    ReadyAtSeconds = atSeconds;
                    break;
                case OrderState.Collected:
                    CollectedAtSeconds = atSeconds;
                    break;
                case OrderState.Paid:
                    PaidAtSeconds = atSeconds;
                    break;
                case OrderState.Abandoned:
                    AbandonedAtSeconds = atSeconds;
                    break;
                case OrderState.Created:
                default:
                    break;
            }
        }
    }
}
