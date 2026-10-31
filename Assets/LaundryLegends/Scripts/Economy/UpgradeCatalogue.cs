using System.Collections.Generic;

namespace LaundryLegends.Economy
{
    /// <summary>
    /// The two upgrades Phase 6 ships with. Kept in code so the HUD, the
    /// machine effects, and tests all read the same ids, costs, and multipliers.
    /// </summary>
    public static class UpgradeCatalogue
    {
        public const string FastDryerId = "fast-dryer";
        public const string FastDryerCapability = "dryer.speed";
        public const float FastDryerDurationMultiplier = 0.7f;

        public const string ExtraWasherId = "extra-washer";
        public const string ExtraWasherCapability = "washer.count";

        /// <summary>
        /// Fast Dryer is a real effect: every dryer cycle lasts 70% as long.
        /// Extra Washer is a progression flag only; it records the purchase and
        /// reserves the capability, but no second machine is spawned yet.
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

        public static UpgradeDefinition ExtraWasher()
        {
            return new UpgradeDefinition(
                ExtraWasherId,
                "Extra Washer",
                "Reserves a second washer bay (progression flag only).",
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
