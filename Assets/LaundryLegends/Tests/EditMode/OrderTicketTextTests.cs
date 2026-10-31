using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Pure text building for the order ticket, so the HUD wording is covered
    /// without rendering IMGUI in a test.
    /// </summary>
    public sealed class OrderTicketTextTests
    {
        [Test]
        public void EmptyLedgerShowsNoActiveOrder()
        {
            var ledger = new OrderLedger(new GameState(0));
            Assert.AreEqual("No active order", OrderTicketView.BuildText(ledger));
        }

        [Test]
        public void NullLedgerIsHandled()
        {
            Assert.AreEqual("No active order", OrderTicketView.BuildText(null));
        }

        [Test]
        public void ActiveOrderShowsIdPriceAndStage()
        {
            var ledger = new OrderLedger(new GameState(0));
            var itemObject = new GameObject("TicketItem");
            LaundryItem item = itemObject.AddComponent<LaundryItem>();
            item.Initialize("test.ticket", LaundryItemKind.LooseLaundry);

            Assert.IsTrue(ledger.TryCreateOrder(item, 40, 0f, out LaundryOrder order));
            Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, 1f));

            string text = OrderTicketView.BuildText(ledger);
            StringAssert.Contains(order.OrderId, text);
            StringAssert.Contains("40", text);
            StringAssert.Contains("waiting for drop-off", text);

            Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, 2f));
            StringAssert.Contains("washing / drying", OrderTicketView.BuildText(ledger));

            Assert.IsTrue(ledger.TryMarkReady(order.OrderId, 3f));
            StringAssert.Contains("ready to collect", OrderTicketView.BuildText(ledger));

            Object.DestroyImmediate(itemObject);
        }
    }
}
