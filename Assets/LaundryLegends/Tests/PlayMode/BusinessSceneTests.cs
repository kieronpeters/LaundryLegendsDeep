using System.Collections;
using LaundryLegends.Business;
using LaundryLegends.Customers;
using LaundryLegends.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>Scene wiring for the Phase 10 business layer.</summary>
    public sealed class BusinessSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShelvesRackClockAndBillsAreWired()
        {
            BusinessClock clock = Object.FindFirstObjectByType<BusinessClock>();
            ConsumableStockService stock = Object.FindFirstObjectByType<ConsumableStockService>();
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            DropOffRack rack = Object.FindFirstObjectByType<DropOffRack>();
            BillingService billing = Object.FindFirstObjectByType<BillingService>();
            BusinessPanelView panel = Object.FindFirstObjectByType<BusinessPanelView>();
            BillsPanelView bills = Object.FindFirstObjectByType<BillsPanelView>();

            Assert.IsNotNull(clock);
            Assert.IsNotNull(stock);
            Assert.IsNotNull(spawner);
            Assert.IsNotNull(rack);
            Assert.IsNotNull(billing);
            Assert.IsNotNull(panel);
            Assert.IsNotNull(bills);

            spawner.SetAutoStart(false);
            spawner.DisposeAll();

            Assert.AreEqual(10, rack.Capacity);
            Assert.AreEqual(10, spawner.MaxCustomers);
            Assert.AreEqual(3, stock.Shelves.Count);
            Assert.AreEqual(5, stock.Count(ConsumableType.Detergent));
            Assert.AreEqual(5, stock.Count(ConsumableType.Fragrance));
            Assert.AreEqual(5, stock.Count(ConsumableType.Plastic));

            StringAssert.Contains("Day 1", panel.BuildText());
            StringAssert.Contains("Detergent", panel.BuildText());

            ConsumableShelf shelf = stock.Find(ConsumableType.Detergent);
            Assert.IsTrue(spawner.Economy.TryEarn(1000, "Phase 10 shelf credit", Time.time));
            Assert.IsTrue(shelf.TryBuyOne(spawner.Economy, Time.time));
            Assert.AreEqual(6, shelf.Count);

            clock.AdvanceMinutes(29 * 1440);
            Assert.IsNotNull(billing.CurrentBill, "A bill should be generated on day 30.");
            Assert.AreEqual(400, billing.CurrentBill.Total);
            StringAssert.Contains("Rent", bills.BuildText());

            Assert.IsTrue(spawner.Economy.TryEarn(1000, "Phase 10 test credit", Time.time));
            int before = spawner.Economy.Balance;
            Assert.IsTrue(billing.TryPayCurrentFromSpawner(Time.time));
            Assert.AreEqual(before - 400, spawner.Economy.Balance);
            Assert.IsNull(billing.CurrentBill);
            yield return null;
        }
    }
}
