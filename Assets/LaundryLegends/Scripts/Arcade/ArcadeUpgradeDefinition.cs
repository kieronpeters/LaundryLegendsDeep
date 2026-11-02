using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// One purchasable arcade upgrade. Effects are business-wide multipliers;
    /// the optional decor object is enabled once the upgrade is owned.
    /// </summary>
    [CreateAssetMenu(menuName = "Laundry Legends/Arcade Upgrade", fileName = "ArcadeUpgrade")]
    public sealed class ArcadeUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "arcade.upgrade.neon";
        [SerializeField] private string _displayName = "Neon sign";
        [SerializeField, Min(0)] private int _cost = 150;
        [SerializeField, Min(0.1f)] private float _incomeMultiplier = 1.1f;
        [SerializeField, Min(0.1f)] private float _trafficMultiplier = 1f;
        [SerializeField] private GameObject _decorObject;
        [SerializeField] private bool _requiresExpansion;

        public string Id => _id;
        public string DisplayName => _displayName;
        public int Cost => _cost;
        public float IncomeMultiplier => _incomeMultiplier;
        public float TrafficMultiplier => _trafficMultiplier;
        public GameObject DecorObject => _decorObject;

        /// <summary>True when the upgrade can only be bought after the bay opens.</summary>
        public bool RequiresExpansion => _requiresExpansion;

        public void Configure(
            string id,
            string displayName,
            int cost,
            float incomeMultiplier,
            float trafficMultiplier,
            GameObject decorObject = null,
            bool requiresExpansion = false)
        {
            _id = id;
            _displayName = displayName;
            _cost = Mathf.Max(0, cost);
            _incomeMultiplier = Mathf.Max(0.1f, incomeMultiplier);
            _trafficMultiplier = Mathf.Max(0.1f, trafficMultiplier);
            _decorObject = decorObject;
            _requiresExpansion = requiresExpansion;
        }
    }
}
