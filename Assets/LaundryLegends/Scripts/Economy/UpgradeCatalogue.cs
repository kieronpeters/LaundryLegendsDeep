using System.Collections.Generic;

namespace LaundryLegends.Economy
{
    /// <summary>
    /// The two upgrades Phase 6 ships with. Kept in code so the HUD, the
    /// machine effects, and tests all read the same ids, costs, and multipliers.
    /// Both upgrades change a real machine timer; neither is flag-only.
    /// </summary>
    public static class UpgradeCatalogue
    {
        public const string FastDryerId = "fast-dryer";
        public const string FastDryerCapability = "dryer.speed";
        public const float FastDryerDurationMultiplier = 0.7f;

        /// <summary>
        /// Stable purchase/save id. It is kept from the earlier flag-only
        /// version so recorded ownership keeps resolving; the display name and
        /// capability now describe the washer speed effect.
        /// </summary>
        public const string ExtraWasherId = "extra-washer";

        public const string ExtraWasherCapability = "washer.speed";
        public const float ExtraWasherDurationMultiplier = 0.75f;

        /// <summary>
        /// Fast Dryer is a real effect: every dryer cycle lasts 70% as long.
        /// </summary>
        public static UpgradeDefinition FastDryer()
        {
            return new UpgradeDefinition(
                FastDryerId,
                "Fast Dryer",
                "Dries every load in 70% of the normal time.",
                120,
                FastDryerCapability);
        }

        /// <summary>
        /// High-Efficiency Washer is a real effect: every washer cycle lasts
        /// 75% as long. The id stays <see cref="ExtraWasherId"/> so existing
        /// ownership and save data remain valid.
        /// </summary>
        public static UpgradeDefinition ExtraWasher()
        {
            return new UpgradeDefinition(
                ExtraWasherId,
                "High-Efficiency Washer",
                "Washes every load in 75% of the normal time.",
                200,
                ExtraWasherCapability);
        }

        /// <summary>Fresh catalogue instances; ownership stays in <see cref="EconomyService"/>.</summary>
        public static IReadOnlyList<UpgradeDefinition> CreateDefault()
        {
            return new List<UpgradeDefinition>
            {
                FastDryer(),
                ExtraWasher()
            };
        }
    }
}
