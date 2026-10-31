using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Proves money actually flows through EconomyService, that tips settle
    /// once, and that the fast-dryer upgrade changes a real machine timer.
    /// </summary>
    public sealed class EconomyIntegrationTests
    {
        private static LaundryItem NewItem()
        {
            var go = new GameObject("EcoItem");
            LaundryItem item = go.AddComponent<LaundryItem>();
            item.Initialize("test.eco", LaundryItemKind.LooseLaundry);
            return item;
        }

        private static LaundryOrder PaidOrder(OrderLedger ledger, LaundryItem item, int price)
        {
            Assert.IsTrue(ledger.TryCreateOrder(item, price, 0f, out LaundryOrder order));
            Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, 1f));
            Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, 2f));
            Assert.IsTrue(ledger.TryMarkReady(order.OrderId, 3f));
            Assert.IsTrue(ledger.TryCollect(order.OrderId, 4f));
            return order;
        }

        [Test]
        public void PaymentAppearsOnceInTheEconomyLog()
        {
            var wallet = new GameState(0);
            var economy = new EconomyService(wallet);
            var ledger = new OrderLedger(wallet, economy);
            LaundryItem item = NewItem();

            LaundryOrder order = PaidOrder(ledger, item, 40);
            Assert.IsTrue(ledger.TryPay(order.OrderId, 5f));

            Assert.AreEqual(40, economy.Balance);
            Assert.AreEqual(1, economy.Transactions.Count);
            Assert.AreEqual(TransactionKind.Income, economy.Transactions[0].Kind);
            StringAssert.Contains("paid", economy.Transactions[0].Reason);

            Assert.IsFalse(ledger.TryPay(order.OrderId, 6f));
            Assert.AreEqual(1, economy.Transactions.Count, "Duplicate payment must not log again.");
            Assert.AreEqual(40, economy.Balance);

            Object.DestroyImmediate(item.gameObject);
        }

        [Test]
        public void TipSettlesExactlyOnce()
        {
            var wallet = new GameState(0);
            var economy = new EconomyService(wallet);
            var ledger = new OrderLedger(wallet, economy);
            LaundryItem item = NewItem();

            LaundryOrder order = PaidOrder(ledger, item, 40);
            Assert.IsTrue(ledger.TryPay(order.OrderId, 5f));

            Assert.IsTrue(ledger.TryAddTip(order.OrderId, 10, 6f));
            Assert.IsFalse(ledger.TryAddTip(order.OrderId, 10, 7f), "A second tip must be rejected.");
            Assert.AreEqual(50, economy.Balance);
            Assert.AreEqual(2, economy.Transactions.Count);

            Object.DestroyImmediate(item.gameObject);
        }

        [Test]
        public void FastDryerUpgradeShortensTheDryerTimer()
        {
            var go = new GameObject("Dryer");
            MachineLoadHook dryer = go.AddComponent<MachineLoadHook>();
            dryer.Configure(null, MachineKind.Dryer);
            dryer.SetDuration(10f);

            Assert.AreEqual(10f, dryer.EffectiveDuration, 0.001f);

            Assert.IsTrue(dryer.ApplyUpgrade(UpgradeCatalogue.FastDryerId, UpgradeCatalogue.FastDryerDurationMultiplier));
            Assert.AreEqual(7f, dryer.EffectiveDuration, 0.001f);
            Assert.IsTrue(dryer.HasAppliedUpgrade(UpgradeCatalogue.FastDryerId));

            Assert.IsFalse(dryer.ApplyUpgrade(UpgradeCatalogue.FastDryerId, 0.5f), "Upgrades apply once.");
            Assert.AreEqual(7f, dryer.EffectiveDuration, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void HighEfficiencyWasherUpgradeShortensTheWasherTimerOnce()
        {
            var go = new GameObject("Washer");
            MachineLoadHook washer = go.AddComponent<MachineLoadHook>();
            washer.Configure(null, MachineKind.Washer);
            washer.SetDuration(20f);

            Assert.AreEqual(20f, washer.EffectiveDuration, 0.001f);

            Assert.IsTrue(washer.ApplyUpgrade(
                UpgradeCatalogue.ExtraWasherId,
                UpgradeCatalogue.ExtraWasherDurationMultiplier));
            Assert.AreEqual(15f, washer.EffectiveDuration, 0.001f);
            Assert.IsTrue(washer.HasAppliedUpgrade(UpgradeCatalogue.ExtraWasherId));

            Assert.IsFalse(washer.ApplyUpgrade(UpgradeCatalogue.ExtraWasherId, 0.5f), "Upgrades apply once.");
            Assert.AreEqual(15f, washer.EffectiveDuration, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void HighEfficiencyWasherDefinitionMatchesTheCatalogue()
        {
            UpgradeDefinition upgrade = UpgradeCatalogue.ExtraWasher();

            Assert.AreEqual(UpgradeCatalogue.ExtraWasherId, upgrade.Id);
            Assert.AreEqual("High-Efficiency Washer", upgrade.DisplayName);
            Assert.AreEqual("washer.speed", upgrade.CapabilityTag);
            Assert.AreEqual(UpgradeCatalogue.ExtraWasherCapability, upgrade.CapabilityTag);
            Assert.AreEqual(200, upgrade.Cost);
            Assert.AreEqual(0.75f, UpgradeCatalogue.ExtraWasherDurationMultiplier, 0.0001f);
            Assert.IsTrue(UpgradeEffects.TryGetMachineEffect(upgrade.Id, out MachineKind kind, out float multiplier));
            Assert.AreEqual(MachineKind.Washer, kind);
            Assert.AreEqual(0.75f, multiplier, 0.0001f);
        }

        [Test]
        public void UpgradeEffectsOnlyTouchMatchingMachinesAndApplyOnce()
        {
            var washerObject = new GameObject("Washer");
            MachineLoadHook washer = washerObject.AddComponent<MachineLoadHook>();
            washer.Configure(null, MachineKind.Washer);
            washer.SetDuration(20f);

            var dryerObject = new GameObject("Dryer");
            MachineLoadHook dryer = dryerObject.AddComponent<MachineLoadHook>();
            dryer.Configure(null, MachineKind.Dryer);
            dryer.SetDuration(20f);

            var machines = new[] { washer, dryer };

            Assert.AreEqual(1, UpgradeEffects.Apply(UpgradeCatalogue.ExtraWasher(), machines));
            Assert.IsTrue(washer.HasAppliedUpgrade(UpgradeCatalogue.ExtraWasherId));
            Assert.IsFalse(dryer.HasAppliedUpgrade(UpgradeCatalogue.ExtraWasherId), "Washer speed must not touch dryers.");
            Assert.AreEqual(15f, washer.EffectiveDuration, 0.001f);
            Assert.AreEqual(20f, dryer.EffectiveDuration, 0.001f);

            Assert.AreEqual(0, UpgradeEffects.Apply(UpgradeCatalogue.ExtraWasher(), machines), "Reapplying is a no-op.");
            Assert.AreEqual(15f, washer.EffectiveDuration, 0.001f);

            Assert.AreEqual(1, UpgradeEffects.Apply(UpgradeCatalogue.FastDryer(), machines));
            Assert.AreEqual(14f, dryer.EffectiveDuration, 0.001f);
            Assert.AreEqual(15f, washer.EffectiveDuration, 0.001f);

            Object.DestroyImmediate(washerObject);
            Object.DestroyImmediate(dryerObject);
        }

        [Test]
        public void UpgradeEffectsIgnoreUnknownUpgrades()
        {
            var go = new GameObject("Washer");
            MachineLoadHook washer = go.AddComponent<MachineLoadHook>();
            washer.Configure(null, MachineKind.Washer);
            washer.SetDuration(20f);

            var flagOnly = new UpgradeDefinition("flag-only", "Flag", "No world effect", 10, "flag");
            Assert.AreEqual(0, UpgradeEffects.Apply(flagOnly, new[] { washer }));
            Assert.AreEqual(20f, washer.EffectiveDuration, 0.001f);

            Assert.AreEqual(0, UpgradeEffects.Apply(null, new[] { washer }));
            Assert.IsFalse(UpgradeEffects.TryGetMachineEffect("flag-only", out _, out _));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SummaryAndGoalTextReflectTheEconomy()
        {
            var wallet = new GameState(100);
            var economy = new EconomyService(wallet);
            economy.TryEarn(40, "Order paid", 1f);

            string summary = OrderTicketView.BuildSummaryText(economy);
            StringAssert.Contains("+40", summary);

            var upgrades = UpgradeCatalogue.CreateDefault();
            string list = OrderTicketView.BuildUpgradeText(economy, upgrades);
            StringAssert.Contains("Fast Dryer", list);

            string goal = OrderTicketView.BuildNextGoalText(economy, upgrades);
            StringAssert.Contains("Fast Dryer", goal);
            StringAssert.Contains("[U]", goal);
        }
    }
}
