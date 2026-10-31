using System;
using System.Collections.Generic;
using LaundryLegends.Core;

namespace LaundryLegends.Economy
{
    public enum TransactionKind
    {
        Income,
        Expense,
        Upgrade
    }

    public readonly struct Transaction
    {
        public Transaction(string id, TransactionKind kind, int amount, string reason, float atSeconds)
        {
            Id = id;
            Kind = kind;
            Amount = amount;
            Reason = reason;
            AtSeconds = atSeconds;
        }

        public string Id { get; }

        public TransactionKind Kind { get; }

        public int Amount { get; }

        public string Reason { get; }

        public float AtSeconds { get; }
    }

    public readonly struct DailySummary
    {
        public DailySummary(int income, int expenses, int upgradeSpend, int transactionCount)
        {
            Income = income;
            Expenses = expenses;
            UpgradeSpend = upgradeSpend;
            TransactionCount = transactionCount;
        }

        public int Income { get; }

        public int Expenses { get; }

        public int UpgradeSpend { get; }

        public int Net => Income - Expenses - UpgradeSpend;

        public int TransactionCount { get; }
    }

    /// <summary>
    /// Single auditable gate for money. Every balance change goes through here
    /// and produces a transaction record, so the summary and the wallet cannot
    /// disagree. Purchases are atomic: all checks happen before any mutation.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly GameState _wallet;
        private readonly List<Transaction> _log = new List<Transaction>();
        private readonly HashSet<string> _purchased = new HashSet<string>(StringComparer.Ordinal);
        private int _nextId = 1;

        public EconomyService(GameState wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public GameState Wallet => _wallet;

        public int Balance => _wallet.Money;

        public IReadOnlyList<Transaction> Transactions => _log;

        public event Action<Transaction> TransactionRecorded;

        public bool HasUpgrade(string upgradeId)
        {
            return !string.IsNullOrEmpty(upgradeId) && _purchased.Contains(upgradeId);
        }

        public bool TryEarn(int amount, string reason, float atSeconds)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(reason))
            {
                return false;
            }

            if (!_wallet.TryCredit(amount))
            {
                return false;
            }

            Record(TransactionKind.Income, amount, reason, atSeconds);
            return true;
        }

        public bool TrySpend(int amount, string reason, float atSeconds)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(reason))
            {
                return false;
            }

            if (!_wallet.TrySpend(amount))
            {
                return false;
            }

            Record(TransactionKind.Expense, amount, reason, atSeconds);
            return true;
        }

        /// <summary>
        /// Buys an upgrade. Fails without side effects when the upgrade is null,
        /// already owned, or unaffordable.
        /// </summary>
        public bool TryPurchase(UpgradeDefinition upgrade, float atSeconds)
        {
            if (upgrade == null || string.IsNullOrEmpty(upgrade.Id))
            {
                return false;
            }

            if (_purchased.Contains(upgrade.Id))
            {
                return false;
            }

            if (upgrade.Cost < 0 || _wallet.Money < upgrade.Cost)
            {
                return false;
            }

            if (upgrade.Cost > 0 && !_wallet.TrySpend(upgrade.Cost))
            {
                return false;
            }

            _purchased.Add(upgrade.Id);
            Record(TransactionKind.Upgrade, upgrade.Cost, $"Upgrade: {upgrade.DisplayName}", atSeconds);
            return true;
        }

        public DailySummary GetSummary()
        {
            int income = 0;
            int expenses = 0;
            int upgrades = 0;
            foreach (Transaction transaction in _log)
            {
                switch (transaction.Kind)
                {
                    case TransactionKind.Income:
                        income += transaction.Amount;
                        break;
                    case TransactionKind.Expense:
                        expenses += transaction.Amount;
                        break;
                    case TransactionKind.Upgrade:
                        upgrades += transaction.Amount;
                        break;
                }
            }

            return new DailySummary(income, expenses, upgrades, _log.Count);
        }

        /// <summary>Cheapest unowned upgrade, used for the "next goal" hint.</summary>
        public UpgradeDefinition FindNextGoal(IEnumerable<UpgradeDefinition> catalogue)
        {
            if (catalogue == null)
            {
                return null;
            }

            UpgradeDefinition best = null;
            foreach (UpgradeDefinition upgrade in catalogue)
            {
                if (upgrade == null || string.IsNullOrEmpty(upgrade.Id) || _purchased.Contains(upgrade.Id))
                {
                    continue;
                }

                if (best == null || upgrade.Cost < best.Cost)
                {
                    best = upgrade;
                }
            }

            return best;
        }

        private void Record(TransactionKind kind, int amount, string reason, float atSeconds)
        {
            var transaction = new Transaction($"TXN-{_nextId++:D4}", kind, amount, reason, atSeconds);
            _log.Add(transaction);
            TransactionRecorded?.Invoke(transaction);
        }
    }
}
