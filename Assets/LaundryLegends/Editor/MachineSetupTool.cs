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
                Material drumMaterial = EnsureMaterial(
                    "Assets/LaundryLegends/Art/Materials/MAT_Drum.mat",
                    "MAT_Drum",
                    new Color(0.06f, 0.065f, 0.075f, 1f),
                    0.45f);
                Material clothesMaterial = EnsureMaterial(
                    "Assets/LaundryLegends/Art/Materials/MAT_DrumClothes.mat",
                    "MAT_DrumClothes",
                    new Color(0.72f, 0.78f, 0.9f, 1f),
                    0.1f);

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
                    EnsureDrum(machine, hook, drumMaterial, clothesMaterial);
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

                if (hook.DrumVisual == null)
                {
                    failures.Add($"Machine_{i} has no rotating drum visual.");
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

        private static void EnsureDrum(
            Transform machine,
            MachineLoadHook hook,
            Material drumMaterial,
            Material clothesMaterial)
        {
            Transform drum = machine.Find("DrumVisual");
            if (drum == null)
            {
                var drumObject = new GameObject("DrumVisual");
                drum = drumObject.transform;
                drum.SetParent(machine, false);
            }

            drum.localPosition = new Vector3(0f, 0.02f, -0.38f);
            drum.localRotation = Quaternion.Euler(90f, 0f, 0f);
            drum.localScale = Vector3.one;

            // The detailed FBX already has a glass door and drum. Keep only the
            // rotating clothes in front of the glass; a synthetic dark disc would
            // cover the realistic model.
            Transform oldFace = drum.Find("DrumFace");
            if (oldFace != null)
            {
                UnityEngine.Object.DestroyImmediate(oldFace.gameObject);
            }

            EnsureDrumCloth(drum, "DrumClothA", new Vector3(0.07f, 0.06f, 0.09f),
                new Vector3(0.2f, 0.16f, 0.09f), clothesMaterial);
            EnsureDrumCloth(drum, "DrumClothB", new Vector3(-0.08f, 0.02f, 0.09f),
                new Vector3(0.18f, 0.14f, 0.09f), clothesMaterial);
            EnsureDrumCloth(drum, "DrumClothC", new Vector3(0.01f, -0.09f, 0.09f),
                new Vector3(0.19f, 0.13f, 0.09f), clothesMaterial);
            hook.SetDrumVisual(drum);
        }

        private static void EnsureDrumCloth(
            Transform drum,
            string name,
            Vector3 localPosition,
            Vector3 scale,
            Material material)
        {
            Transform existing = drum.Find(name);
            GameObject cube = existing != null
                ? existing.gameObject
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            cube.name = name;
            cube.transform.SetParent(drum, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = scale;
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            cube.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material EnsureMaterial(string path, string name, Color colour, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(lit) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = lit;
            }

            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
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
