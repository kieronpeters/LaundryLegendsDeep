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

        // Money intentionally has no public mutator. EconomyService will be the
        // single writer when transactions are introduced; until then it is read-only.

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
    }
}
