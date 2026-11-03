using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Single validation entry point for the scene/asset tools. It performs no
    /// writes; each validator opens the scene read-only and throws on failure.
    /// </summary>
    public static class ProjectValidationTool
    {
        [MenuItem("Laundry Legends/Phase 16/Run All Validations")]
        public static void RunAll()
        {
            var failures = new List<string>();
            Run("Business", BusinessSetupTool.ValidateBusinessSystems, failures);
            Run("Appliances", ApplianceIntegrationTool.Validate, failures);
            Run("NPC animation", NpcAnimationSetupTool.ValidateNpcAnimation, failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Validation failed ({failures.Count}): {string.Join("; ", failures)}");
            }

            Debug.Log("[VALIDATE] Business, appliance and NPC animation validators passed.");
        }

        private static void Run(string label, Action action, List<string> failures)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failures.Add($"{label}: {exception.Message}");
                Debug.LogError($"[VALIDATE] {label} failed: {exception}");
            }
        }
    }
}
