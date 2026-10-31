using System;
using System.Collections.Generic;

namespace LaundryLegends.Core
{
    /// <summary>
    /// Runtime state owned by the player profile: money, day/time, and progression flags.
    /// A single instance is created by <see cref="GameSession"/> and lives until the application quits.
    /// This type holds no static mutable state.
    /// </summary>
    public sealed class GameState
    {
        private readonly HashSet<string> _progressionFlags = new HashSet<string>();

        public GameState(int startingMoney = 0)
        {
            if (startingMoney < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingMoney), "Starting money cannot be negative.");
            }

            Money = startingMoney;
        }

        public int Money { get; private set; }

        public int Day { get; private set; } = 1;

        public float TimeOfDayHours { get; private set; } = 8f;

        public IReadOnlyCollection<string> ProgressionFlags => _progressionFlags;

        // Money has no public setter. All movement goes through TrySpend and
        // TryCredit so every change is guarded and reviewable in one place.

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrWhiteSpace(flag) && _progressionFlags.Contains(flag);
        }

        public void SetFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
            {
                throw new ArgumentException("Flag name cannot be empty.", nameof(flag));
            }

            _progressionFlags.Add(flag);
        }

        public void AdvanceDay()
        {
            Day += 1;
        }

        /// <summary>
        /// Deducts <paramref name="amount"/> when the player can afford it.
        /// Returns false and leaves <see cref="Money"/> unchanged otherwise.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
        public bool TrySpend(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Spend amount cannot be negative.");
            }

            if (amount > Money)
            {
                return false;
            }

            Money -= amount;
            return true;
        }

        /// <summary>
        /// True when a credit of <paramref name="amount"/> is representable.
        /// Callers check this before committing a payment so a successful
        /// transaction can never silently skip the money movement.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
        public bool CanCredit(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Credit amount cannot be negative.");
            }

            return Money <= int.MaxValue - amount;
        }

        /// <summary>
        /// Adds <paramref name="amount"/> to <see cref="Money"/>. Returns false
        /// and leaves the balance unchanged when the result would overflow.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
        public bool TryCredit(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Credit amount cannot be negative.");
            }

            if (!CanCredit(amount))
            {
                return false;
            }

            Money += amount;
            return true;
        }
    }
}
