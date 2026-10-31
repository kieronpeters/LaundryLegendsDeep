using System;
using UnityEngine;

namespace LaundryLegends.Economy
{
    /// <summary>
    /// Data for one purchasable upgrade. Ownership lives in
    /// <see cref="EconomyService"/>; this type is pure data so it can be
    /// authored as an asset or built in a test.
    /// </summary>
    [Serializable]
    public sealed class UpgradeDefinition
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private int _cost;
        [SerializeField] private string _capabilityTag = string.Empty;

        public UpgradeDefinition()
        {
        }

        public UpgradeDefinition(string id, string displayName, string description, int cost, string capabilityTag)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _cost = cost;
            _capabilityTag = capabilityTag;
        }

        public string Id => _id;

        public string DisplayName => _displayName;

        public string Description => _description;

        public int Cost => _cost;

        /// <summary>Short flag other systems read to enable a capability.</summary>
        public string CapabilityTag => _capabilityTag;

        public override string ToString()
        {
            return $"{_displayName} ({_cost})";
        }
    }
}
