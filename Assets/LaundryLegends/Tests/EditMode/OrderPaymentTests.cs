using System;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the money guard: the wallet API on
    /// <see cref="GameState"/> and the exactly-once payment it backs inside
    /// <see cref="OrderLedger"/>.
    /// </summary>
    public sealed class OrderPaymentTests
    {
        [Test]
        public void TrySpendDeductsWhenAffordable()
        {
            var state = new GameState(100);

            Assert.IsTrue(state.TrySpend(30));
            Assert.AreEqual(70, state.Money);

            Assert.IsTrue(state.TrySpend(70));
            Assert.AreEqual(0, state.Money);
        }

        [Test]
        public void TrySpendLeavesMoneyUntouchedWhenUnaffordable()
        {
            var state = new GameState(20);

            Assert.IsFalse(state.TrySpend(21));
            Assert.AreEqual(20, state.Money);
        }

        [Test]
        public void TrySpendRejectsNegativeAmount()
        {
            var state = new GameState(20);

            Assert.Throws<ArgumentOutOfRangeException>(() => state.TrySpend(-1));
            Assert.AreEqual(20, state.Money);
        }

        [Test]
        public void TryCreditAddsMoney()
        {
            var state = new GameState(20);

            Assert.IsTrue(state.TryCredit(5));
            Assert.IsTrue(state.TryCredit(15));
            Assert.AreEqual(40, state.Money);
        }

        [Test]
        public void TryCreditRejectsNegativeAmount()
        {
            var state = new GameState(20);

            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryCredit(-5));
            Assert.AreEqual(20, state.Money);
        }

        [Test]
        public void TryCreditRejectsOverflowWithoutChangingBalance()
        {
            var state = new GameState(int.MaxValue);

            Assert.IsFalse(state.CanCredit(1));
            Assert.IsFalse(state.TryCredit(1));
            Assert.AreEqual(int.MaxValue, state.Money);

            var empty = new GameState(0);
            Assert.IsTrue(empty.TryCredit(int.MaxValue));
            Assert.AreEqual(int.MaxValue, empty.Money);
        }

        [Test]
        public void DuplicateLedgerPaymentCreditsExactlyOnce()
        {
            var wallet = new GameState(0);
            var ledger = new OrderLedger(wallet);
            var itemObject = new GameObject("PaymentItem");

            try
            {
                LaundryItem item = itemObject.AddComponent<LaundryItem>();
                item.Initialize("payment-item", LaundryItemKind.LooseLaundry);

                Assert.IsTrue(ledger.TryCreateOrder(item, 25, 0f, out LaundryOrder order), ledger.LastResult);
                Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, 1f), ledger.LastResult);
                Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, 2f), ledger.LastResult);
                Assert.IsTrue(ledger.TryMarkReady(order.OrderId, 3f), ledger.LastResult);
                Assert.IsTrue(ledger.TryCollect(order.OrderId, 4f), ledger.LastResult);

                Assert.IsTrue(ledger.TryPay(order.OrderId, 5f), ledger.LastResult);
                Assert.AreEqual(25, wallet.Money);

                Assert.IsFalse(ledger.TryPay(order.OrderId, 6f));
                Assert.AreEqual(25, wallet.Money, "A duplicate payment attempt must not credit money.");

                Assert.IsFalse(ledger.TryPay(order.OrderId, 7f));
                Assert.AreEqual(25, wallet.Money, "Repeated payment attempts must stay rejected.");
                Assert.AreEqual(OrderState.Paid, order.State);
            }
            finally
            {
                Object.DestroyImmediate(itemObject);
            }
        }

        [Test]
        public void ZeroPriceOrderSettlesExactlyOnceWithoutMovingMoney()
        {
            var wallet = new GameState(10);
            var ledger = new OrderLedger(wallet);
            var itemObject = new GameObject("FreeOrderItem");

            try
            {
                LaundryItem item = itemObject.AddComponent<LaundryItem>();
                item.Initialize("free-item", LaundryItemKind.LooseLaundry);

                Assert.IsTrue(ledger.TryCreateOrder(item, 0, 0f, out LaundryOrder order), ledger.LastResult);
                Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, 1f), ledger.LastResult);
                Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, 2f), ledger.LastResult);
                Assert.IsTrue(ledger.TryMarkReady(order.OrderId, 3f), ledger.LastResult);
                Assert.IsTrue(ledger.TryCollect(order.OrderId, 4f), ledger.LastResult);
                Assert.IsTrue(ledger.TryPay(order.OrderId, 5f), ledger.LastResult);

                Assert.AreEqual(10, wallet.Money);
                Assert.IsFalse(ledger.TryPay(order.OrderId, 6f));
                Assert.AreEqual(10, wallet.Money);
            }
            finally
            {
                Object.DestroyImmediate(itemObject);
            }
        }
    }
}
