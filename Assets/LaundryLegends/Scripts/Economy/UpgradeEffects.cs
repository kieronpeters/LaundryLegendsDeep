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

            if (!TryGetMachineEffect(upgrade.Id, out MachineKind kind, out float durationMultiplier))
            {
                return 0;
            }

            int applied = 0;
            foreach (MachineLoadHook machine in machines)
            {
                if (machine == null || machine.Kind != kind)
                {
                    continue;
                }

                if (machine.ApplyUpgrade(upgrade.Id, durationMultiplier))
                {
                    applied += 1;
                }
            }

            return applied;
        }

        /// <summary>
        /// Capability map for the catalogue. Returns false for an unknown or
        /// flag-only upgrade, in which case nothing in the scene changes.
        /// </summary>
        public static bool TryGetMachineEffect(
            string upgradeId,
            out MachineKind kind,
            out float durationMultiplier)
        {
            switch (upgradeId)
            {
                case UpgradeCatalogue.FastDryerId:
                    kind = MachineKind.Dryer;
                    durationMultiplier = UpgradeCatalogue.FastDryerDurationMultiplier;
                    return true;

                case UpgradeCatalogue.ExtraWasherId:
                    kind = MachineKind.Washer;
                    durationMultiplier = UpgradeCatalogue.ExtraWasherDurationMultiplier;
                    return true;

                default:
                    kind = MachineKind.Washer;
                    durationMultiplier = 1f;
                    return false;
            }
        }
    }
}
