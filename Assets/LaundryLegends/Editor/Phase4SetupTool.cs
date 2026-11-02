using System;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>Adds the Phase 4 dryer hook and folding/wrap station additively.</summary>
    public static class Phase4SetupTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";

        [MenuItem("Laundry Legends/Phase 4/Build Dryer and Folding")]
        public static void BuildDryerAndFolding()
        {
            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform machine = FindChild(room.transform, "Machine_2");
                if (machine == null)
                {
                    throw new InvalidOperationException("Missing Machine_2.");
                }

                Transform dryerSlot = machine.Find("DryerSlot");
                if (dryerSlot == null)
                {
                    var slotObject = new GameObject("DryerSlot");
                    dryerSlot = slotObject.transform;
                    dryerSlot.SetParent(machine, false);
                    dryerSlot.localPosition = new Vector3(0f, -0.4f, -0.1f);
                }

                CarrySlot slot = dryerSlot.GetComponent<CarrySlot>();
                if (slot == null)
                {
                    slot = dryerSlot.gameObject.AddComponent<CarrySlot>();
                }

                slot.AcceptAllKinds();
                MachineLoadHook dryer = machine.GetComponent<MachineLoadHook>();
                if (dryer == null)
                {
                    dryer = machine.gameObject.AddComponent<MachineLoadHook>();
                }

                dryer.Configure(slot, MachineKind.Dryer);

                Transform table = FindChild(room.transform, "FoldingTable");
                if (table == null)
                {
                    throw new InvalidOperationException("Missing FoldingTable.");
                }

                Transform foldSlot = table.Find("FoldSlot");
                if (foldSlot == null)
                {
                    var slotObject = new GameObject("FoldSlot");
                    foldSlot = slotObject.transform;
                    foldSlot.SetParent(table, false);
                    foldSlot.localPosition = new Vector3(0f, 0.6f, 0f);
                }

                CarrySlot foldCarry = foldSlot.GetComponent<CarrySlot>();
                if (foldCarry == null)
                {
                    foldCarry = foldSlot.gameObject.AddComponent<CarrySlot>();
                }

                foldCarry.AcceptAllKinds();
                FoldingStation folding = table.GetComponent<FoldingStation>();
                if (folding == null)
                {
                    folding = table.gameObject.AddComponent<FoldingStation>();
                }

                folding.Configure(foldCarry);

                Transform counter = FindChild(room.transform, "Counter");
                if (counter != null)
                {
                    Transform dropOff = counter.Find("DropOffSlot");
                    if (dropOff == null)
                    {
                        var slotObject = new GameObject("DropOffSlot");
                        dropOff = slotObject.transform;
                        dropOff.SetParent(counter, false);
                        dropOff.localPosition = new Vector3(0f, 0.65f, 0f);
                    }

                    CarrySlot dropCarry = dropOff.GetComponent<CarrySlot>();
                    if (dropCarry == null)
                    {
                        dropCarry = dropOff.gameObject.AddComponent<CarrySlot>();
                    }

                    dropCarry.AcceptAllKinds();
                }

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase4] Dryer hook and folding/wrap station built.");
        }

        [MenuItem("Laundry Legends/Phase 4/Validate Dryer and Folding")]
        public static void ValidateDryerAndFolding()
        {
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            MachineLoadHook[] hooks = room.GetComponentsInChildren<MachineLoadHook>(true);
            int washers = 0;
            int dryers = 0;
            foreach (MachineLoadHook hook in hooks)
            {
                if (hook.Kind == MachineKind.Washer) washers++;
                else dryers++;
            }

            if (washers != 4 || dryers != 4)
            {
                throw new InvalidOperationException($"Expected four washers and four dryers, found {washers}/{dryers}.");
            }

            FoldingStation folding = room.GetComponentInChildren<FoldingStation>(true);
            if (folding == null || folding.GetComponentInChildren<CarrySlot>(true) == null)
            {
                throw new InvalidOperationException("Folding station or its slot is missing.");
            }

            Debug.Log("[Phase4] Dryer/folding validation passed.");
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChild(root.GetChild(i), name);
                if (match != null) return match;
            }

            return null;
        }
    }
}
