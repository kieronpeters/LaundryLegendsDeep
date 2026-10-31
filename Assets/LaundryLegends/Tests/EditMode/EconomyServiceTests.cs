using LaundryLegends.Core;
using LaundryLegends.Economy;
using NUnit.Framework;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class EconomyServiceTests
    {
        private static EconomyService NewService(int money = 100)
        {
            return new EconomyService(new GameState(money));
        }

        [Test]
        public void EarningIncreasesBalanceAndLogs()
        {
            EconomyService economy = NewService(100);
            Assert.IsTrue(economy.TryEarn(40, "Order paid", 1f));
            Assert.AreEqual(140, economy.Balance);
            Assert.AreEqual(1, economy.Transactions.Count);
            Assert.AreEqual(TransactionKind.Income, economy.Transactions[0].Kind);
        }

        [Test]
        public void SpendingMoreThanBalanceFailsWithoutSideEffects()
        {
            EconomyService economy = NewService(50);
            Assert.IsFalse(economy.TrySpend(80, "Repairs", 1f));
            Assert.AreEqual(50, economy.Balance);
            Assert.AreEqual(0, economy.Transactions.Count);
        }

        [Test]
        public void UpgradesCostMoneyAndApplyOnce()
        {
            EconomyService economy = NewService(200);
            var upgrade = new UpgradeDefinition("fast-dryer", "Fast Dryer", "Dries faster", 120, "dryer.speed");

            Assert.IsTrue(economy.TryPurchase(upgrade, 1f));
            Assert.AreEqual(80, economy.Balance);
            Assert.IsTrue(economy.HasUpgrade("fast-dryer"));

            Assert.IsFalse(economy.TryPurchase(upgrade, 2f), "Second purchase must be rejected.");
            Assert.AreEqual(80, economy.Balance, "A rejected purchase must not charge again.");
            Assert.AreEqual(1, economy.Transactions.Count);
        }

        [Test]
        public void UnaffordableUpgradeIsRejectedAtomically()
        {
            EconomyService economy = NewService(50);
            var upgrade = new UpgradeDefinition(
                UpgradeCatalogue.ExtraWasherId,
                "High-Efficiency Washer",
                "Washes faster",
                200,
                UpgradeCatalogue.ExtraWasherCapability);

            Assert.IsFalse(economy.TryPurchase(upgrade, 1f));
            Assert.AreEqual(50, economy.Balance);
            Assert.IsFalse(economy.HasUpgrade(UpgradeCatalogue.ExtraWasherId));
            Assert.AreEqual(0, economy.Transactions.Count);
        }

        [Test]
        public void DailyOverheadDeductsOnceAndLogsOnce()
        {
            EconomyService economy = NewService(100);

            Assert.IsTrue(DailyOverhead.TryCharge(economy, 5f));
            Assert.AreEqual(85, economy.Balance);
            Assert.AreEqual(1, economy.Transactions.Count);

            Transaction charge = economy.Transactions[0];
            Assert.AreEqual(TransactionKind.Expense, charge.Kind);
            Assert.AreEqual(DailyOverhead.DefaultAmount, charge.Amount);
            Assert.AreEqual(5f, charge.AtSeconds, 0.0001f);
            StringAssert.Contains("overhead", charge.Reason);
            Assert.AreEqual(15, economy.GetSummary().Expenses);
        }

        [Test]
        public void UnaffordableDailyOverheadLeavesBalanceAndLogUntouched()
        {
            EconomyService economy = NewService(10);

            Assert.IsFalse(DailyOverhead.TryCharge(economy, 5f));
            Assert.AreEqual(10, economy.Balance);
            Assert.AreEqual(0, economy.Transactions.Count);
            Assert.AreEqual(0, economy.GetSummary().Expenses);
        }

        [Test]
        public void DailyOverheadCanEmptyTheWalletButNotOverdrawIt()
        {
            EconomyService economy = NewService(20);

            Assert.IsTrue(DailyOverhead.TryCharge(economy, 1f));
            Assert.AreEqual(5, economy.Balance);

            Assert.IsFalse(DailyOverhead.TryCharge(economy, 2f));
            Assert.AreEqual(5, economy.Balance);
            Assert.AreEqual(1, economy.Transactions.Count);
        }

        [Test]
        public void DailyOverheadRejectsMissingEconomyAndNonPositiveAmount()
        {
            Assert.IsFalse(DailyOverhead.TryCharge(null, 1f));

            EconomyService economy = NewService(100);
            Assert.IsFalse(DailyOverhead.TryCharge(economy, 1f, 0));
            Assert.IsFalse(DailyOverhead.TryCharge(economy, 1f, -15));
            Assert.AreEqual(100, economy.Balance);
            Assert.AreEqual(0, economy.Transactions.Count);
        }

        [Test]
        public void FreeUpgradeStillRecordsOnce()
        {
            EconomyService economy = NewService(0);
            var upgrade = new UpgradeDefinition("welcome", "Welcome Kit", "Free", 0, "kit");

            Assert.IsTrue(economy.TryPurchase(upgrade, 1f));
            Assert.IsFalse(economy.TryPurchase(upgrade, 2f));
            Assert.AreEqual(1, economy.Transactions.Count);
        }

        [Test]
        public void SummaryMatchesTheTransactionLog()
        {
            EconomyService economy = NewService(100);
            economy.TryEarn(40, "Order paid", 1f);
            economy.TryEarn(15, "Tip", 2f);
            economy.TrySpend(10, "Detergent", 3f);
            economy.TryPurchase(new UpgradeDefinition("u1", "Upgrade", "x", 25, "tag"), 4f);

            DailySummary summary = economy.GetSummary();
            Assert.AreEqual(55, summary.Income);
            Assert.AreEqual(10, summary.Expenses);
            Assert.AreEqual(25, summary.UpgradeSpend);
            Assert.AreEqual(20, summary.Net);
            Assert.AreEqual(4, summary.TransactionCount);
            Assert.AreEqual(120, economy.Balance);
        }

        [Test]
        public void TransactionIdsAreUnique()
        {
            EconomyService economy = NewService(100);
            economy.TryEarn(1, "a", 0f);
            economy.TryEarn(1, "b", 0f);
            economy.TrySpend(1, "c", 0f);
            Assert.AreNotEqual(economy.Transactions[0].Id, economy.Transactions[1].Id);
            Assert.AreNotEqual(economy.Transactions[1].Id, economy.Transactions[2].Id);
        }

        [Test]
        public void NextGoalIsTheCheapestUnownedUpgrade()
        {
            EconomyService economy = NewService(500);
            var cheap = new UpgradeDefinition("a", "Cheap", "x", 100, "a");
            var mid = new UpgradeDefinition("b", "Mid", "x", 300, "b");
            var none = new UpgradeDefinition("c", "None", "x", 900, "c");

            economy.TryPurchase(cheap, 0f);

            UpgradeDefinition next = economy.FindNextGoal(new[] { cheap, mid, none });
            Assert.AreSame(mid, next);
        }

        [Test]
        public void InvalidAmountsAreRejected()
        {
            EconomyService economy = NewService(100);
            Assert.IsFalse(economy.TryEarn(0, "nothing", 0f));
            Assert.IsFalse(economy.TryEarn(-5, "negative", 0f));
            Assert.IsFalse(economy.TrySpend(0, "nothing", 0f));
            Assert.AreEqual(100, economy.Balance);
            Assert.AreEqual(0, economy.Transactions.Count);
        }
    }
}
