using System;
using System.Collections.Generic;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Gives every appliance on the eight-machine wall a working load slot and a
    /// <see cref="MachineLoadHook"/>. Odd machines are washers, even machines are
    /// dryers, matching the authored body materials. Replaces the old one-washer /
    /// one-dryer prototype wiring.
    /// </summary>
    public static class MachineSetupTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const int MachineCount = 8;

        [MenuItem("Laundry Legends/Phase 9/Build All Machines")]
        public static void BuildAllMachines()
        {
            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                for (int i = 1; i <= MachineCount; i++)
                {
                    Transform machine = FindChild(room.transform, $"Machine_{i}");
                    if (machine == null)
                    {
                        throw new InvalidOperationException($"GreyboxRoom has no Machine_{i}.");
                    }

                    MachineKind kind = i % 2 == 1 ? MachineKind.Washer : MachineKind.Dryer;
                    Transform slotTransform = EnsureSlot(machine, kind);
                    CarrySlot slot = slotTransform.GetComponent<CarrySlot>();
                    if (slot == null)
                    {
                        slot = slotTransform.gameObject.AddComponent<CarrySlot>();
                    }

                    slot.AcceptAllKinds();
                    SetAnchor(slot, slotTransform);

                    MachineLoadHook hook = machine.GetComponent<MachineLoadHook>();
                    if (hook == null)
                    {
                        hook = machine.gameObject.AddComponent<MachineLoadHook>();
                    }

                    hook.Configure(slot, kind);
                    hook.enabled = true;
                }

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MACHINES] Built 8 working machines: 4 washers and 4 dryers.");
        }

        [MenuItem("Laundry Legends/Phase 9/Validate All Machines")]
        public static void ValidateAllMachines()
        {
            var failures = new List<string>();
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (room == null)
            {
                throw new InvalidOperationException($"Missing room prefab: {RoomPrefabPath}");
            }

            MachineLoadHook[] hooks = room.GetComponentsInChildren<MachineLoadHook>(true);
            if (hooks.Length != MachineCount)
            {
                failures.Add($"Expected {MachineCount} MachineLoadHooks, found {hooks.Length}.");
            }

            int washers = 0;
            int dryers = 0;
            for (int i = 1; i <= MachineCount; i++)
            {
                Transform machine = FindChild(room.transform, $"Machine_{i}");
                if (machine == null)
                {
                    failures.Add($"Missing Machine_{i}.");
                    continue;
                }

                MachineLoadHook hook = machine.GetComponent<MachineLoadHook>();
                if (hook == null)
                {
                    failures.Add($"Machine_{i} has no MachineLoadHook.");
                    continue;
                }

                MachineKind expected = i % 2 == 1 ? MachineKind.Washer : MachineKind.Dryer;
                if (hook.Kind != expected)
                {
                    failures.Add($"Machine_{i} is {hook.Kind}, expected {expected}.");
                }

                if (hook.Slot == null)
                {
                    failures.Add($"Machine_{i} has no load slot.");
                }
                else if (hook.Slot.GetComponent<CarrySlot>() == null)
                {
                    failures.Add($"Machine_{i}'s load slot has no CarrySlot.");
                }

                if (expected == MachineKind.Washer) washers++;
                else dryers++;
            }

            if (washers != 4 || dryers != 4)
            {
                failures.Add($"Expected 4 washers and 4 dryers, found {washers}/{dryers}.");
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[MACHINES] {failure}");
                }

                throw new InvalidOperationException($"Machine validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[MACHINES] Validation passed: 4 washers and 4 dryers, each with a load slot.");
        }

        private static Transform EnsureSlot(Transform machine, MachineKind kind)
        {
            string preferred = kind == MachineKind.Washer ? "DrumSlot" : "DryerSlot";
            Transform slot = machine.Find(preferred);
            if (slot == null)
            {
                // Keep an existing slot even if an earlier prototype gave it the
                // other name; otherwise create the preferred one.
                slot = machine.Find("DrumSlot");
                if (slot == null)
                {
                    slot = machine.Find("DryerSlot");
                }
            }

            if (slot == null)
            {
                var slotObject = new GameObject(preferred);
                slot = slotObject.transform;
                slot.SetParent(machine, false);
                slot.localPosition = kind == MachineKind.Washer
                    ? new Vector3(0f, 0f, -0.1f)
                    : new Vector3(0f, -0.4f, -0.1f);
                slot.localRotation = Quaternion.identity;
                slot.localScale = Vector3.one;
            }

            return slot;
        }

        private static void SetAnchor(CarrySlot slot, Transform anchor)
        {
            var serialized = new SerializedObject(slot);
            SerializedProperty property = serialized.FindProperty("_anchor");
            if (property != null)
            {
                property.objectReferenceValue = anchor;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChild(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
