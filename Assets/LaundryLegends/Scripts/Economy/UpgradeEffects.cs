using System.Collections.Generic;
using LaundryLegends.Laundry;

namespace LaundryLegends.Economy
{
    /// <summary>
    /// Bridges an owned upgrade to the world objects it changes. This is the one
    /// place that knows which capability maps to which machine behaviour, so the
    /// purchase UI and the customer spawner stay effect-agnostic.
    /// </summary>
    public static class UpgradeEffects
    {
        /// <summary>
        /// Applies <paramref name="upgrade"/> to every eligible machine.
        /// Idempotent: a machine that already has the upgrade is left alone.
        /// Returns how many machines accepted the effect.
        /// </summary>
        public static int Apply(UpgradeDefinition upgrade, IEnumerable<MachineLoadHook> machines)
        {
            if (upgrade == null || string.IsNullOrEmpty(upgrade.Id) || machines == null)
            {
                return 0;
            }

            if (upgrade.Id != UpgradeCatalogue.FastDryerId)
            {
                // Extra Washer and any future flag-only upgrade change state in
                // EconomyService only; there is no world effect to apply.
                return 0;
            }

            int applied = 0;
            foreach (MachineLoadHook machine in machines)
            {
                if (machine == null || machine.Kind != MachineKind.Dryer)
                {
                    continue;
                }

                if (machine.ApplyUpgrade(upgrade.Id, UpgradeCatalogue.FastDryerDurationMultiplier))
                {
                    applied += 1;
                }
            }

            return applied;
        }
    }
}
