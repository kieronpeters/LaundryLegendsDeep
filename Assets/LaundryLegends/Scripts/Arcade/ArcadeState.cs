using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    public enum ArcadePhase
    {
        Off,
        Installing,
        Active
    }

    /// <summary>
    /// Serializable arcade business state. It holds only data and the phase
    /// rules, so a later save system can persist it as-is while the scene-facing
    /// <see cref="ArcadeService"/> performs economy and scene work.
    /// </summary>
    [Serializable]
    public sealed class ArcadeState
    {
        [SerializeField] private ArcadePhase _phase = ArcadePhase.Off;
        [SerializeField] private int _totalIncome;
        [SerializeField] private int _visitCount;
        [SerializeField] private int _nextVisitNumber = 1;
        [SerializeField] private List<string> _paidVisits = new List<string>();

        /// <summary>Current machine lifecycle phase.</summary>
        public ArcadePhase Phase => _phase;

        /// <summary>True once the machine has been bought, including while installing.</summary>
        public bool IsInstalled => _phase != ArcadePhase.Off;

        /// <summary>True only when the machine is installed and powered.</summary>
        public bool IsActive => _phase == ArcadePhase.Active;

        /// <summary>Every arcade income credit recorded so far.</summary>
        public int TotalIncome => _totalIncome;

        /// <summary>Paid visits recorded so far.</summary>
        public int VisitCount => _visitCount;

        /// <summary>Number used for the next stable visit/transaction id.</summary>
        public int NextVisitNumber => _nextVisitNumber;

        /// <summary>Visit ids that have already been paid, in recording order.</summary>
        public IReadOnlyList<string> PaidVisits => _paidVisits;

        public bool HasPaidVisit(string visitId)
        {
            return !string.IsNullOrWhiteSpace(visitId) && _paidVisits.Contains(visitId);
        }

        /// <summary>
        /// Undoes a recorded visit whose income credit was rejected, so the state
        /// never claims money the wallet did not receive.
        /// </summary>
        public bool ForgetVisit(string visitId, int income)
        {
            if (string.IsNullOrWhiteSpace(visitId) || !_paidVisits.Remove(visitId))
            {
                return false;
            }

            _visitCount = Mathf.Max(0, _visitCount - 1);
            _totalIncome = Mathf.Max(0, _totalIncome - income);
            return true;
        }

        /// <summary>Moves Off to Installing. Returns false when already installed.</summary>
        public bool BeginInstall()
        {
            if (IsInstalled)
            {
                return false;
            }

            _phase = ArcadePhase.Installing;
            return true;
        }

        /// <summary>Moves Installing to Active. Returns false from any other phase.</summary>
        public bool Activate()
        {
            if (_phase != ArcadePhase.Installing)
            {
                return false;
            }

            _phase = ArcadePhase.Active;
            return true;
        }

        /// <summary>
        /// Creates the next unique visit id, skipping ids already recorded as
        /// paid so a restored state cannot hand out a colliding id.
        /// </summary>
        public string CreateVisitId()
        {
            string id;
            do
            {
                id = $"arcade-visit-{_nextVisitNumber:D4}";
                _nextVisitNumber++;
            }
            while (HasPaidVisit(id));

            return id;
        }

        /// <summary>
        /// Records a paid visit. Returns false for a missing id, a non-positive
        /// amount, an amount that would overflow the total, or an id that has
        /// already been paid, leaving the state intact.
        /// </summary>
        public bool RecordVisit(string visitId, int income)
        {
            if (string.IsNullOrWhiteSpace(visitId) || income <= 0 || HasPaidVisit(visitId))
            {
                return false;
            }

            if (_totalIncome > int.MaxValue - income)
            {
                return false;
            }

            _paidVisits.Add(visitId);
            _visitCount++;
            _totalIncome += income;

            // Keep the dedupe list bounded: only recent visits need replay protection.
            const int maxTrackedVisits = 512;
            if (_paidVisits.Count > maxTrackedVisits)
            {
                _paidVisits.RemoveAt(0);
            }

            return true;
        }
    }
}
