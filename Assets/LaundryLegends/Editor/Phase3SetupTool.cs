using System;
using System.Collections.Generic;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds and validates the Phase 3 placeholder laundry items and their
    /// additive scene placement. Every method is idempotent.
    /// </summary>
    public static class Phase3SetupTool
    {
        private const string LaundryPrefabFolder = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry";
        private const string BasketPath = LaundryPrefabFolder + "/Basket.prefab";
        private const string LooseLaundryPath = LaundryPrefabFolder + "/LooseLaundry.prefab";
        private const string BagPath = LaundryPrefabFolder + "/Bag.prefab";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string LaundromatScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string ItemsRootName = "Phase3Items";
        private const string MachineHookTargetName = "Machine_1";
        private const string DrumSlotName = "DrumSlot";

        private static readonly (string Label, Vector3 Start, Vector3 End)[] PublicRoutes =
        {
            ("entrance-to-counter", new Vector3(0f, 1.1f, -4.1f), new Vector3(-2.7f, 1.1f, -2.4f)),
            ("central aisle", new Vector3(0f, 1.1f, -3.8f), new Vector3(0f, 1.1f, 3.8f)),
            ("machine aisle", new Vector3(0f, 1.1f, 3.8f), new Vector3(5.8f, 1.1f, 3.8f)),
            ("east circulation", new Vector3(6.4f, 1.1f, 3.8f), new Vector3(6.7f, 1.1f, -3.6f)),
            ("waiting-to-central", new Vector3(6.7f, 1.1f, -3.6f), new Vector3(0f, 1.1f, -3.6f)),
            ("central-to-entrance", new Vector3(0f, 1.1f, -3.6f), new Vector3(0f, 1.1f, -4.1f)),
            ("counter-to-entrance", new Vector3(-2.7f, 1.1f, -2.4f), new Vector3(0f, 1.1f, -4.1f))
        };

        [MenuItem("Laundry Legends/Phase 3/Build Items")]
        public static void BuildItems()
        {
            EnsureFolder("Assets/LaundryLegends/Prefabs/Gameplay", "Laundry");

            Material basketMaterial = RequireMaterial("Grey_Metal");
            Material laundryMaterial = RequireMaterial("Grey_Chair");
            Material bagMaterial = RequireMaterial("Grey_Wall");

            BuildBasket(basketMaterial);
            BuildLooseLaundry(laundryMaterial);
            BuildBag(bagMaterial);

            EnsureItemPickup(BasketPath);
            EnsureItemPickup(LooseLaundryPath);
            EnsureItemPickup(BagPath);

            ConfigureSceneItems();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3] Placeholder laundry prefabs and additive scene placement built.");
        }

        [MenuItem("Laundry Legends/Phase 3/Setup Item Pickup")]
        public static void SetupItemPickup()
        {
            EnsureItemPickup(BasketPath);
            EnsureItemPickup(LooseLaundryPath);
            EnsureItemPickup(BagPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3] Item pickup interaction components ensured on all laundry prefabs.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Items")]
        public static void ValidateItems()
        {
            var failures = new List<string>();
            var templateIds = new HashSet<string>();

            ValidatePrefab(BasketPath, LaundryItemKind.Basket, false, templateIds, failures);
            ValidatePrefab(LooseLaundryPath, LaundryItemKind.LooseLaundry, true, templateIds, failures);
            ValidatePrefab(BagPath, LaundryItemKind.Bag, false, templateIds, failures);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                if (FindRoot(scene, "GreyboxRoom") == null)
                {
                    failures.Add("Laundromat has no GreyboxRoom instance.");
                }

                if (FindRoot(scene, "Player") == null)
                {
                    failures.Add("Laundromat has no Player instance.");
                }

                ValidateSceneItems(scene, failures);
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Phase3] {failure}");
                }

                throw new InvalidOperationException($"Phase 3 item validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Phase3] Item validation passed: prefabs, identity, colliders, materials, scene placement, and route clearance are valid.");
        }

        [MenuItem("Laundry Legends/Phase 3/Build Machine Hook")]
        public static void BuildMachineHook()
        {
            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform machine = FindChild(room.transform, MachineHookTargetName);
                if (machine == null)
                {
                    throw new InvalidOperationException($"GreyboxRoom has no {MachineHookTargetName} child.");
                }

                foreach (MachineLoadHook existing in room.GetComponentsInChildren<MachineLoadHook>(true))
                {
                    if (existing.gameObject != machine.gameObject)
                    {
                        UnityEngine.Object.DestroyImmediate(existing);
                    }
                }

                Transform drum = machine.Find(DrumSlotName);
                if (drum == null)
                {
                    var drumObject = new GameObject(DrumSlotName);
                    drum = drumObject.transform;
                    drum.SetParent(machine, false);
                    drum.localPosition = new Vector3(0f, 0f, -0.1f);
                    drum.localRotation = Quaternion.identity;
                    drum.localScale = Vector3.one;
                }

                CarrySlot slot = drum.GetComponent<CarrySlot>();
                if (slot == null)
                {
                    slot = drum.gameObject.AddComponent<CarrySlot>();
                }

                slot.SetAcceptedKinds(LaundryItemKind.LooseLaundry);
                var slotSerialized = new SerializedObject(slot);
                SerializedProperty anchor = slotSerialized.FindProperty("_anchor");
                if (anchor == null)
                {
                    throw new InvalidOperationException("Serialized field not found: _anchor on CarrySlot");
                }

                anchor.objectReferenceValue = drum;
                slotSerialized.ApplyModifiedPropertiesWithoutUndo();

                MachineLoadHook hook = machine.GetComponent<MachineLoadHook>();
                if (hook == null)
                {
                    hook = machine.gameObject.AddComponent<MachineLoadHook>();
                }

                hook.Configure(slot);

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3] Washer load/unload hook built additively on Machine_1.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Machine Hook")]
        public static void ValidateMachineHook()
        {
            var failures = new List<string>();
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (room == null)
            {
                failures.Add($"Missing room prefab: {RoomPrefabPath}");
            }
            else
            {
                Transform machine = FindChild(room.transform, MachineHookTargetName);
                if (machine == null)
                {
                    failures.Add($"GreyboxRoom has no {MachineHookTargetName} child.");
                }
                else
                {
                    ValidateMachineGeometry(machine, failures);

                    MachineLoadHook[] hooks = room.GetComponentsInChildren<MachineLoadHook>(true);
                    if (hooks.Length != 1)
                    {
                        failures.Add($"Expected exactly one MachineLoadHook, found {hooks.Length}.");
                    }
                    else if (hooks[0].gameObject != machine.gameObject)
                    {
                        failures.Add($"MachineLoadHook must be on {MachineHookTargetName}, found on {hooks[0].gameObject.name}.");
                    }
                    else
                    {
                        MachineLoadHook hook = hooks[0];
                        if (hook.Slot == null)
                        {
                            failures.Add("MachineLoadHook has no CarrySlot.");
                        }
                        else
                        {
                            Transform drum = machine.Find(DrumSlotName);
                            if (drum == null || hook.Slot.gameObject != drum.gameObject)
                            {
                                failures.Add($"MachineLoadHook slot must be the {MachineHookTargetName}/{DrumSlotName} child.");
                            }

                            if (!hook.Slot.AcceptsKind(LaundryItemKind.LooseLaundry))
                            {
                                failures.Add("Machine slot must accept loose laundry.");
                            }

                            if (hook.Slot.AcceptsKind(LaundryItemKind.Basket)
                                || hook.Slot.AcceptsKind(LaundryItemKind.Bag))
                            {
                                failures.Add("Machine slot must reject baskets and bags.");
                            }
                        }
                    }

                    CarrySlot[] slots = room.GetComponentsInChildren<CarrySlot>(true);
                    if (slots.Length != 1)
                    {
                        failures.Add($"Expected exactly one machine CarrySlot, found {slots.Length}.");
                    }
                }
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                MachineLoadHook[] sceneHooks = FindSceneComponents<MachineLoadHook>(scene);
                if (sceneHooks.Length != 1)
                {
                    failures.Add($"Laundromat should contain exactly one MachineLoadHook, found {sceneHooks.Length}.");
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Phase3] {failure}");
                }

                throw new InvalidOperationException($"Phase 3 machine-hook validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Phase3] Machine-hook validation passed: one washer hook, loose-laundry-only slot, and unchanged machine geometry.");
        }

        private static void ValidateMachineGeometry(Transform machine, List<string> failures)
        {
            Vector3 expectedPosition = new Vector3(-3.15f, 0.55f, 5.25f);
            Vector3 expectedScale = new Vector3(0.8f, 1.1f, 0.7f);

            if ((machine.localPosition - expectedPosition).sqrMagnitude > 0.0001f)
            {
                failures.Add($"{MachineHookTargetName} moved from its authored position: {machine.localPosition}.");
            }

            if ((machine.localScale - expectedScale).sqrMagnitude > 0.0001f)
            {
                failures.Add($"{MachineHookTargetName} was rescaled: {machine.localScale}.");
            }

            if (machine.GetComponent<Collider>() == null)
            {
                failures.Add($"{MachineHookTargetName} lost its collider.");
            }

            if (machine.Find(DrumSlotName) == null)
            {
                failures.Add($"{MachineHookTargetName} has no {DrumSlotName} child.");
            }
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found.ToArray();
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

        private static void BuildBasket(Material material)
        {
            var root = new GameObject("Basket");
            LaundryItem item = root.AddComponent<LaundryItem>();
            item.Initialize("laundry.basket", LaundryItemKind.Basket);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.15f, 0f);
            collider.size = new Vector3(0.5f, 0.3f, 0.35f);

            CreateBody(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.3f, 0.35f), material);
            AddBody(root);

            PrefabUtility.SaveAsPrefabAsset(root, BasketPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildLooseLaundry(Material material)
        {
            var root = new GameObject("LooseLaundry");
            LaundryItem item = root.AddComponent<LaundryItem>();
            item.Initialize("laundry.loose", LaundryItemKind.LooseLaundry);

            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.center = new Vector3(0f, 0.15f, 0f);
            collider.radius = 0.16f;

            CreateBody(root.transform, PrimitiveType.Sphere, "Body", new Vector3(0f, 0.15f, 0f), new Vector3(0.32f, 0.32f, 0.32f), material);
            AddBody(root);

            PrefabUtility.SaveAsPrefabAsset(root, LooseLaundryPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildBag(Material material)
        {
            var root = new GameObject("Bag");
            LaundryItem item = root.AddComponent<LaundryItem>();
            item.Initialize("laundry.bag", LaundryItemKind.Bag);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.25f, 0f);
            collider.size = new Vector3(0.36f, 0.5f, 0.24f);

            CreateBody(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.36f, 0.5f, 0.24f), material);
            AddBody(root);

            PrefabUtility.SaveAsPrefabAsset(root, BagPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject CreateBody(
            Transform parent,
            PrimitiveType primitive,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject body = GameObject.CreatePrimitive(primitive);
            body.name = name;
            Collider generatedCollider = body.GetComponent<Collider>();
            if (generatedCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(generatedCollider);
            }

            body.transform.SetParent(parent, false);
            body.transform.localPosition = localPosition;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = localScale;
            body.GetComponent<Renderer>().sharedMaterial = material;
            return body;
        }

        private static void AddBody(GameObject root)
        {
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.useGravity = true;
            body.isKinematic = false;
        }

        private static void EnsureItemPickup(string prefabPath)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                LaundryItem item = prefab.GetComponent<LaundryItem>();
                if (item == null)
                {
                    throw new InvalidOperationException($"{prefabPath} has no LaundryItem.");
                }

                LaundryItemInteractable[] existing = prefab.GetComponents<LaundryItemInteractable>();
                for (int i = 1; i < existing.Length; i++)
                {
                    UnityEngine.Object.DestroyImmediate(existing[i]);
                }

                LaundryItemInteractable pickup = existing.Length > 0
                    ? existing[0]
                    : prefab.AddComponent<LaundryItemInteractable>();
                pickup.Configure(item);

                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void ConfigureSceneItems()
        {
            Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
            GameObject existing = FindRoot(scene, ItemsRootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var root = new GameObject(ItemsRootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            Place(BasketPath, root.transform, new Vector3(3.2f, 0.05f, 2.6f), "Basket_01");
            Place(BagPath, root.transform, new Vector3(4.4f, 0.05f, 2.6f), "Bag_01");
            Place(LooseLaundryPath, root.transform, new Vector3(3.7f, 0.05f, 2.45f), "LooseLaundry_01");
            Place(LooseLaundryPath, root.transform, new Vector3(4.05f, 0.05f, 2.45f), "LooseLaundry_02");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Place(string prefabPath, Transform parent, Vector3 position, string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing item prefab: {prefabPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        }

        private static void ValidatePrefab(
            string path,
            LaundryItemKind expectedKind,
            bool expectedWasherLoadable,
            HashSet<string> templateIds,
            List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                failures.Add($"Missing item prefab: {path}");
                return;
            }

            LaundryItem item = prefab.GetComponent<LaundryItem>();
            if (item == null)
            {
                failures.Add($"{path}: missing LaundryItem.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(item.TemplateId))
                {
                    failures.Add($"{path}: blank TemplateId.");
                }
                else if (!templateIds.Add(item.TemplateId))
                {
                    failures.Add($"{path}: duplicate TemplateId '{item.TemplateId}'.");
                }

                if (item.Kind != expectedKind)
                {
                    failures.Add($"{path}: expected kind {expectedKind}, found {item.Kind}.");
                }

                if (item.IsWasherLoadable != expectedWasherLoadable)
                {
                    failures.Add($"{path}: washer-loadable should be {expectedWasherLoadable}.");
                }
            }

            if (prefab.GetComponentInChildren<Collider>(true) == null)
            {
                failures.Add($"{path}: missing collider.");
            }

            LaundryItemInteractable[] pickups = prefab.GetComponents<LaundryItemInteractable>();
            if (pickups.Length != 1)
            {
                failures.Add($"{path}: expected exactly one LaundryItemInteractable, found {pickups.Length}.");
            }
            else if (pickups[0].Item != item)
            {
                failures.Add($"{path}: pickup interaction is not wired to its LaundryItem.");
            }

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == null)
                {
                    failures.Add($"{path}: renderer '{renderer.gameObject.name}' has no material.");
                }
            }
        }

        private static void ValidateSceneItems(Scene scene, List<string> failures)
        {
            GameObject root = FindRoot(scene, ItemsRootName);
            if (root == null)
            {
                failures.Add($"Laundromat has no {ItemsRootName} root.");
                return;
            }

            LaundryItem[] items = root.GetComponentsInChildren<LaundryItem>(true);
            int baskets = 0;
            int loose = 0;
            int bags = 0;

            foreach (LaundryItem item in items)
            {
                if (item == null)
                {
                    failures.Add($"{ItemsRootName} contains a missing LaundryItem reference.");
                    continue;
                }

                switch (item.Kind)
                {
                    case LaundryItemKind.Basket:
                        baskets++;
                        break;
                    case LaundryItemKind.LooseLaundry:
                        loose++;
                        break;
                    case LaundryItemKind.Bag:
                        bags++;
                        break;
                }

                if (item.GetComponentInChildren<Collider>(true) == null)
                {
                    failures.Add($"{item.name}: scene instance has no collider.");
                }

                if (item.GetComponent<LaundryItemInteractable>() == null)
                {
                    failures.Add($"{item.name}: scene instance has no pickup interaction.");
                }

                Vector3 point = new Vector3(item.transform.position.x, 1.1f, item.transform.position.z);
                foreach (var route in PublicRoutes)
                {
                    if (DistanceToSegment(point, route.Start, route.End) < 0.55f)
                    {
                        failures.Add($"{item.name} blocks the {route.Label} route.");
                    }
                }
            }

            if (baskets < 1)
            {
                failures.Add($"{ItemsRootName} needs at least one basket.");
            }

            if (bags < 1)
            {
                failures.Add($"{ItemsRootName} needs at least one bag.");
            }

            if (loose < 2)
            {
                failures.Add($"{ItemsRootName} needs at least two loose-laundry items.");
            }
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return Vector3.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return Vector3.Distance(point, start + segment * t);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static Material RequireMaterial(string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing material: {path}");
            }

            return material;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
