using System;

namespace LaundryLegends.Progression
{
    public enum SecretRoomStage
    {
        Hidden,
        Revealed,
        Cleaned,
        Powered,
        Repaired,
        Usable
    }

    public readonly struct SecretRoomSnapshot
    {
        public SecretRoomSnapshot(int clues, int completedOrders, SecretRoomStage stage)
        {
            Clues = clues;
            CompletedOrders = completedOrders;
            Stage = stage;
        }

        public int Clues { get; }

        public int CompletedOrders { get; }

        public SecretRoomStage Stage { get; }
    }

    /// <summary>
    /// Gating and preparation for the hidden room. Kept free of scene code so
    /// the threshold rules can be tested directly; the scene layer only reads
    /// <see cref="Stage"/> and the eligibility flags.
    /// </summary>
    public sealed class SecretRoomState
    {
        private readonly int _requiredClues;
        private readonly int _requiredOrders;
        private int _clues;
        private int _completedOrders;

        public SecretRoomState(int requiredClues = 3, int requiredOrders = 3, int startingClues = 0, int startingOrders = 0)
        {
            _requiredClues = Math.Max(1, requiredClues);
            _requiredOrders = Math.Max(1, requiredOrders);
            _clues = Math.Max(0, startingClues);
            _completedOrders = Math.Max(0, startingOrders);
        }

        public int Clues => _clues;

        public int RequiredClues => _requiredClues;

        public int CompletedOrders => _completedOrders;

        public int RequiredOrders => _requiredOrders;

        public SecretRoomStage Stage { get; private set; } = SecretRoomStage.Hidden;

        public bool IsEligible => _clues >= _requiredClues && _completedOrders >= _requiredOrders;

        public bool HasClue => _clues > 0;

        /// <summary>Records a clue; returns false once every clue is known.</summary>
        public bool TryAddClue()
        {
            if (_clues >= _requiredClues)
            {
                return false;
            }

            _clues++;
            return true;
        }

        public void RecordCompletedOrder()
        {
            _completedOrders++;
        }

        /// <summary>Reveals the room only when the eligibility threshold is met.</summary>
        public bool TryReveal()
        {
            if (Stage != SecretRoomStage.Hidden || !IsEligible)
            {
                return false;
            }

            Stage = SecretRoomStage.Revealed;
            return true;
        }

        /// <summary>Advances preparation one step: Cleaned, Powered, Repaired, Usable.</summary>
        public bool TryPrepare()
        {
            switch (Stage)
            {
                case SecretRoomStage.Revealed:
                    Stage = SecretRoomStage.Cleaned;
                    return true;
                case SecretRoomStage.Cleaned:
                    Stage = SecretRoomStage.Powered;
                    return true;
                case SecretRoomStage.Powered:
                    Stage = SecretRoomStage.Repaired;
                    return true;
                case SecretRoomStage.Repaired:
                    Stage = SecretRoomStage.Usable;
                    return true;
                default:
                    return false;
            }
        }

        public SecretRoomSnapshot Snapshot()
        {
            return new SecretRoomSnapshot(_clues, _completedOrders, Stage);
        }

        /// <summary>Restores a saved state, ignoring out-of-range values.</summary>
        public static SecretRoomState Restore(SecretRoomSnapshot snapshot, int requiredClues = 3, int requiredOrders = 3)
        {
            var state = new SecretRoomState(requiredClues, requiredOrders, snapshot.Clues, snapshot.CompletedOrders)
            {
                Stage = snapshot.Stage
            };
            return state;
        }
    }
}
