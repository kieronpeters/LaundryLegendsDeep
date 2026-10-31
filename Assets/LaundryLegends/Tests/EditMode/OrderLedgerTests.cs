using System;
using System.Collections.Generic;
using System.Reflection;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the order model and ledger: the transition table,
    /// immutable price, id uniqueness, and exactly-once payment.
    /// </summary>
    public sealed class OrderLedgerTests
    {
        private static readonly OrderState[] Lifecycle =
        {
            OrderState.Created,
            OrderState.Accepted,
            OrderState.Processing,
            OrderState.Ready,
            OrderState.Collected,
            OrderState.Paid
        };

        private readonly List<GameObject> _created = new List<GameObject>();

        private GameState _wallet;
        private OrderLedger _ledger;
        private int _nextOrderNumber;

        [SetUp]
        public void CreateLedger()
        {
            _wallet = new GameState(100);
            _ledger = new OrderLedger(_wallet);
            _nextOrderNumber = 0;
        }

        [TearDown]
        public void DestroyCreated()
        {
            foreach (GameObject created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        [Test]
        public void EveryLegalTransitionSucceeds()
        {
            Assert.AreEqual(9, LaundryOrder.TransitionRules.Count, "The transition table changed; update the tests with it.");

            foreach (var rule in LaundryOrder.TransitionRules)
            {
                LaundryOrder order = CreateOrderAt(rule.From);
                float at = order.StateChangedAtSeconds + 1f;

                Assert.IsTrue(
                    order.TryTransition(rule.To, at),
                    $"{rule.From} -> {rule.To} should be legal but was rejected: {order.LastTransitionError}");
                Assert.AreEqual(rule.To, order.State);
                Assert.AreEqual(at, order.StateChangedAtSeconds);
                Assert.IsEmpty(order.LastTransitionError);
            }
        }

        [Test]
        public void IllegalTransitionsAreRejectedAndLeaveStateUnchanged()
        {
            int rejected = 0;

            foreach (OrderState from in (OrderState[])Enum.GetValues(typeof(OrderState)))
            {
                foreach (OrderState to in (OrderState[])Enum.GetValues(typeof(OrderState)))
                {
                    if (LaundryOrder.IsTransitionAllowed(from, to))
                    {
                        continue;
                    }

                    LaundryOrder order = CreateOrderAt(from);
                    OrderState stateBefore = order.State;
                    float changedAt = order.StateChangedAtSeconds;

                    Assert.IsFalse(
                        order.TryTransition(to, changedAt + 5f),
                        $"{from} -> {to} must be rejected.");
                    Assert.AreEqual(stateBefore, order.State, $"{from} -> {to} mutated the state.");
                    Assert.AreEqual(changedAt, order.StateChangedAtSeconds, $"{from} -> {to} mutated the timestamp.");
                    Assert.IsNotEmpty(order.LastTransitionError);
                    rejected += 1;
                }
            }

            Assert.AreEqual(40, rejected, "Expected 49 state pairs minus the 9 legal ones.");
        }

        [Test]
        public void PriceCannotChangeAfterCreation()
        {
            LaundryOrder order = CreateOrderAt(OrderState.Created, 25);

            PropertyInfo price = typeof(LaundryOrder).GetProperty(nameof(LaundryOrder.Price));

            Assert.IsNotNull(price);
            Assert.IsNull(price.GetSetMethod(true), "Price must have no setter, not even a private one.");
            Assert.AreEqual(25, order.Price);
        }

        [Test]
        public void PayingTwiceCreditsMoneyOnce()
        {
            Assert.AreEqual(100, _wallet.Money);

            LaundryOrder order = CreateOrderAt(OrderState.Collected, 30);

            Assert.IsTrue(_ledger.TryPay(order.OrderId, 50f), _ledger.LastResult);
            Assert.AreEqual(130, _wallet.Money);
            Assert.AreEqual(OrderState.Paid, order.State);
            Assert.AreEqual(50f, order.PaidAtSeconds.GetValueOrDefault(-1f));
            Assert.AreEqual(1, _ledger.CompletedOrders.Count);
            Assert.AreEqual(0, _ledger.ActiveOrders.Count);

            Assert.IsFalse(_ledger.TryPay(order.OrderId, 60f), "A duplicate payment must be rejected.");
            Assert.AreEqual(130, _wallet.Money, "A rejected duplicate payment must not credit money again.");
            Assert.AreEqual(50f, order.PaidAtSeconds.GetValueOrDefault(-1f), "A rejected payment must not move timestamps.");
            StringAssert.Contains("already paid", _ledger.LastResult);
        }

        [Test]
        public void PayingAnOrderThatWasNeverCollectedCreditsNothing()
        {
            LaundryOrder order = CreateOrderAt(OrderState.Ready, 30);

            Assert.IsFalse(_ledger.TryPay(order.OrderId, 10f));
            Assert.AreEqual(OrderState.Ready, order.State);
            Assert.AreEqual(100, _wallet.Money);
            Assert.IsNull(order.PaidAtSeconds);
        }

        [Test]
        public void GeneratedOrderIdsAreUnique()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(_ledger.TryCreateOrder(CreateItem(), 10, 0f, out LaundryOrder order), _ledger.LastResult);
                Assert.IsTrue(seen.Add(order.OrderId), $"Duplicate order id '{order.OrderId}'.");
            }

            Assert.AreEqual(5, _ledger.Count);
        }

        [Test]
        public void RegisteringADuplicateOrderIdFails()
        {
            LaundryOrder first = CreateOrderAt(OrderState.Created);
            var duplicate = new LaundryOrder(first.OrderId, CreateItem(), 10, 0f);

            Assert.IsFalse(_ledger.TryRegisterOrder(duplicate));
            Assert.AreEqual(1, _ledger.Count);
            StringAssert.Contains(first.OrderId, _ledger.LastResult);
        }

        [Test]
        public void SecondActiveOrderForTheSameItemIsRejected()
        {
            LaundryItem item = CreateItem();
            Assert.IsTrue(_ledger.TryCreateOrder(item, 10, 0f, out _), _ledger.LastResult);
            Assert.IsFalse(_ledger.TryCreateOrder(item, 10, 1f, out LaundryOrder rejected));
            Assert.IsNull(rejected);
            Assert.AreEqual(1, _ledger.Count);
        }

        [Test]
        public void CollectingBeforeReadyFails()
        {
            LaundryOrder order = CreateOrderAt(OrderState.Accepted);

            Assert.IsFalse(_ledger.TryCollect(order.OrderId, 10f));
            Assert.AreEqual(OrderState.Accepted, order.State);
            Assert.IsNull(order.CollectedAtSeconds);
            StringAssert.Contains("Accepted", _ledger.LastResult);
        }

        [Test]
        public void MarkingReadySkipsProcessingNever()
        {
            LaundryOrder order = CreateOrderAt(OrderState.Accepted);

            Assert.IsFalse(_ledger.TryMarkReady(order.OrderId, 10f), "Accepted must pass through processing first.");
            Assert.AreEqual(OrderState.Accepted, order.State);
            Assert.IsTrue(_ledger.TryMarkProcessing(order.OrderId, 11f), _ledger.LastResult);
            Assert.IsTrue(_ledger.TryMarkReady(order.OrderId, 12f), _ledger.LastResult);
            Assert.AreEqual(OrderState.Ready, order.State);
        }

        [Test]
        public void UnknownOrderIdFailsCleanly()
        {
            Assert.IsFalse(_ledger.TryAcceptOrder("missing", 1f));
            StringAssert.Contains("missing", _ledger.LastResult);

            Assert.IsFalse(_ledger.TryPay(null, 1f));
            Assert.IsFalse(_ledger.TryCollect(string.Empty, 1f));
            Assert.IsFalse(_ledger.TryMarkReady("   ", 1f));

            Assert.IsFalse(_ledger.TryGetOrder("missing", out LaundryOrder none));
            Assert.IsNull(none);
            Assert.AreEqual(100, _wallet.Money);
            Assert.AreEqual(0, _ledger.Count);
        }

        [Test]
        public void AbandonedOrderLeavesTheActiveListAndCannotBePaid()
        {
            LaundryOrder order = CreateOrderAt(OrderState.Ready);

            Assert.IsTrue(_ledger.TryAbandon(order.OrderId, 20f), _ledger.LastResult);
            Assert.AreEqual(OrderState.Abandoned, order.State);
            Assert.AreEqual(20f, order.AbandonedAtSeconds.GetValueOrDefault(-1f));
            Assert.AreEqual(0, _ledger.ActiveOrders.Count);
            Assert.AreEqual(1, _ledger.CompletedOrders.Count);

            Assert.IsFalse(_ledger.TryPay(order.OrderId, 21f));
            Assert.AreEqual(100, _wallet.Money);
        }

        [Test]
        public void LedgerDrivesTheFullLifecycleAndCreditsOnce()
        {
            LaundryItem item = CreateItem();

            Assert.IsTrue(_ledger.TryCreateOrder(item, 40, 0f, out LaundryOrder order), _ledger.LastResult);
            Assert.AreEqual(OrderState.Created, order.State);
            Assert.AreSame(order, _ledger.FindOrderForItem(item));

            Assert.IsTrue(_ledger.TryAcceptOrder(order.OrderId, 1f), _ledger.LastResult);
            Assert.IsTrue(_ledger.TryMarkProcessing(order.OrderId, 2f), _ledger.LastResult);
            Assert.IsTrue(_ledger.TryMarkReady(order.OrderId, 3f), _ledger.LastResult);
            Assert.IsTrue(_ledger.TryCollect(order.OrderId, 4f), _ledger.LastResult);
            Assert.IsTrue(_ledger.TryPay(order.OrderId, 5f), _ledger.LastResult);

            Assert.AreEqual(OrderState.Paid, order.State);
            Assert.AreEqual(140, _wallet.Money);
            Assert.AreEqual(1f, order.AcceptedAtSeconds.GetValueOrDefault(-1f));
            Assert.AreEqual(2f, order.ProcessingAtSeconds.GetValueOrDefault(-1f));
            Assert.AreEqual(3f, order.ReadyAtSeconds.GetValueOrDefault(-1f));
            Assert.AreEqual(4f, order.CollectedAtSeconds.GetValueOrDefault(-1f));
            Assert.AreEqual(5f, order.PaidAtSeconds.GetValueOrDefault(-1f));
            Assert.AreEqual(1, _ledger.CompletedOrders.Count);
            Assert.AreEqual(0, _ledger.ActiveOrders.Count);
            Assert.AreEqual(1, _ledger.AllOrders.Count);
        }

        /// <summary>Builds a registered order and walks the canonical chain to <paramref name="state"/>.</summary>
        private LaundryOrder CreateOrderAt(OrderState state, int price = 10)
        {
            _nextOrderNumber += 1;
            var order = new LaundryOrder($"ORD-T{_nextOrderNumber:D4}", CreateItem(), price, 0f);

            Assert.IsTrue(_ledger.TryRegisterOrder(order), _ledger.LastResult);

            if (state == OrderState.Abandoned)
            {
                Assert.IsTrue(order.TryTransition(OrderState.Abandoned, 1f), order.LastTransitionError);
                return order;
            }

            int index = Array.IndexOf(Lifecycle, state);
            Assert.GreaterOrEqual(index, 0, $"No lifecycle path to {state}.");

            for (int i = 1; i <= index; i++)
            {
                float at = order.StateChangedAtSeconds + 1f;
                Assert.IsTrue(
                    order.TryTransition(Lifecycle[i], at),
                    $"{Lifecycle[i - 1]} -> {Lifecycle[i]} failed: {order.LastTransitionError}");
            }

            Assert.AreEqual(state, order.State);
            return order;
        }

        private LaundryItem CreateItem()
        {
            var target = new GameObject("OrderItem");
            _created.Add(target);
            LaundryItem item = target.AddComponent<LaundryItem>();
            item.Initialize("order-item", LaundryItemKind.LooseLaundry);
            return item;
        }
    }
}
