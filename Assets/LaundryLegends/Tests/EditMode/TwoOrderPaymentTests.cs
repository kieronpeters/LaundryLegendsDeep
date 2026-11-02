using System.Collections.Generic;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Regression cover for the Phase 8 playtest blocker "the wallet never
    /// reaches the shelf price": two complete orders in one session must each
    /// credit the wallet, and a repeated attempt at the first order must not
    /// consume the second order's payout.
    /// </summary>
    public sealed class TwoOrderPaymentTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void CleanUp()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _created.Clear();
        }

        [Test]
        public void TwoConsecutiveOrdersCreditTheWalletTwice()
        {
            var wallet = new GameState(100);
            var economy = new EconomyService(wallet);
            var ledger = new OrderLedger(wallet, economy);

            LaundryOrder first = CompleteOrder(ledger, 40, 1f);
            Assert.IsTrue(ledger.TryPay(first.OrderId, 6f), ledger.LastResult);
            Assert.AreEqual(140, wallet.Money, "The first order should credit its price once.");

            LaundryOrder second = CompleteOrder(ledger, 40, 20f);
            Assert.IsTrue(ledger.TryPay(second.OrderId, 26f), ledger.LastResult);

            Assert.AreEqual(180, wallet.Money, "Two paid orders must credit the wallet twice.");
            Assert.AreEqual(80, PaidIncome(economy), "The economy log should record two payouts.");
            Assert.AreEqual(2, PayoutCount(economy), "The economy log should show exactly two payouts.");

            Assert.AreEqual(2, ledger.CompletedOrders.Count);
            Assert.AreEqual(OrderState.Paid, first.State);
            Assert.AreEqual(OrderState.Paid, second.State);
            Assert.AreNotEqual(first.OrderId, second.OrderId, "Each customer gets its own ticket.");
            Assert.AreEqual(2, ledger.AllOrders.Count);
        }

        [Test]
        public void RepeatingTheFirstPaymentDoesNotBlockTheSecondPayout()
        {
            var wallet = new GameState(100);
            var economy = new EconomyService(wallet);
            var ledger = new OrderLedger(wallet, economy);

            LaundryOrder first = CompleteOrder(ledger, 40, 1f);
            Assert.IsTrue(ledger.TryPay(first.OrderId, 6f), ledger.LastResult);

            // A stale HUD button or a second customer paying with the old id.
            Assert.IsFalse(ledger.TryPay(first.OrderId, 7f), "A settled order must stay settled.");
            Assert.AreEqual(140, wallet.Money, "The duplicate attempt must not move money.");

            LaundryOrder second = CompleteOrder(ledger, 40, 20f);
            Assert.IsTrue(ledger.TryPay(second.OrderId, 26f), ledger.LastResult);

            Assert.AreEqual(180, wallet.Money, "The second order must still pay after a duplicate attempt.");
            Assert.AreEqual(2, PayoutCount(economy));
        }

        private LaundryOrder CompleteOrder(OrderLedger ledger, int price, float startSeconds)
        {
            var itemObject = new GameObject("TwoOrderItem");
            _created.Add(itemObject);
            LaundryItem item = itemObject.AddComponent<LaundryItem>();
            item.Initialize($"two-order-{startSeconds:0}", LaundryItemKind.LooseLaundry);

            Assert.IsTrue(ledger.TryCreateOrder(item, price, startSeconds, out LaundryOrder order), ledger.LastResult);
            Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, startSeconds + 1f), ledger.LastResult);
            Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, startSeconds + 2f), ledger.LastResult);
            Assert.IsTrue(ledger.TryMarkReady(order.OrderId, startSeconds + 3f), ledger.LastResult);
            Assert.IsTrue(ledger.TryCollect(order.OrderId, startSeconds + 4f), ledger.LastResult);
            return order;
        }

        private static int PayoutCount(EconomyService economy)
        {
            int count = 0;
            foreach (Transaction transaction in economy.Transactions)
            {
                if (transaction.Kind == TransactionKind.Income
                    && transaction.Reason.IndexOf("paid", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int PaidIncome(EconomyService economy)
        {
            int total = 0;
            foreach (Transaction transaction in economy.Transactions)
            {
                if (transaction.Kind == TransactionKind.Income
                    && transaction.Reason.IndexOf("paid", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    total += transaction.Amount;
                }
            }

            return total;
        }
    }
}
