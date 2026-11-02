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
        [SerializeField] private List<string> _upgrades = new List<string>();
        [SerializeField] private List<string> _milestones = new List<string>();
        [SerializeField] private float _incomeMultiplier = 1f;
        [SerializeField] private float _trafficMultiplier = 1f;

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

        /// <summary>Upgrade ids owned by the business, in purchase order.</summary>
        public IReadOnlyList<string> Upgrades => _upgrades;

        /// <summary>Milestone ids already earned, in earning order.</summary>
        public IReadOnlyList<string> Milestones => _milestones;

        /// <summary>Business-wide income multiplier from owned upgrades.</summary>
        public float IncomeMultiplier => _incomeMultiplier;

        /// <summary>Business-wide traffic multiplier from owned upgrades.</summary>
        public float TrafficMultiplier => _trafficMultiplier;

        public bool HasPaidVisit(string visitId)
        {
            return !string.IsNullOrWhiteSpace(visitId) && _paidVisits.Contains(visitId);
        }

        public bool HasUpgrade(string upgradeId)
        {
            return !string.IsNullOrWhiteSpace(upgradeId) && _upgrades.Contains(upgradeId);
        }

        public bool HasMilestone(string milestoneId)
        {
            return !string.IsNullOrWhiteSpace(milestoneId) && _milestones.Contains(milestoneId);
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

        /// <summary>Creates the next unique visit id using the legacy prefix.</summary>
        public string CreateVisitId()
        {
            return CreateVisitId("arcade-visit");
        }

        /// <summary>
        /// Creates the next unique visit id for <paramref name="prefix"/>, skipping
        /// ids already recorded so a restored state cannot hand out a collision.
        /// </summary>
        public string CreateVisitId(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "arcade-visit";
            }

            string id;
            do
            {
                id = $"{prefix}-{_nextVisitNumber:D4}";
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

        /// <summary>
        /// Records one upgrade and folds its multipliers into the business totals.
        /// A duplicate id is rejected without changing anything.
        /// </summary>
        public bool RecordUpgrade(string upgradeId, float incomeMultiplier, float trafficMultiplier)
        {
            if (string.IsNullOrWhiteSpace(upgradeId) || HasUpgrade(upgradeId))
            {
                return false;
            }

            _upgrades.Add(upgradeId);
            _incomeMultiplier = Mathf.Max(0.01f, _incomeMultiplier * Mathf.Max(0.01f, incomeMultiplier));
            _trafficMultiplier = Mathf.Max(0.01f, _trafficMultiplier * Mathf.Max(0.01f, trafficMultiplier));
            return true;
        }

        /// <summary>Records one milestone exactly once.</summary>
        public bool RecordMilestone(string milestoneId)
        {
            if (string.IsNullOrWhiteSpace(milestoneId) || HasMilestone(milestoneId))
            {
                return false;
            }

            _milestones.Add(milestoneId);
            return true;
        }
    }
}
