using System.Collections.Generic;
using System.IO;
using LaundryLegends.Business;
using LaundryLegends.Customers;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the Phase 10 business layer in the Laundromat scene: three consumable
    /// shelves, a ten-slot order rack, the clock, billing, HUD and bills menu.
    /// Idempotent: it replaces only the objects it owns.
    /// </summary>
    public static class BusinessSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string DefinitionFolder = "Assets/LaundryLegends/ScriptableObjects/Business";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const int ShelfStock = 5;
        private const int OrderSlots = 10;

        [MenuItem("Laundry Legends/Phase 10/Build Business Systems")]
        public static void BuildBusinessSystems()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            var staff = Object.FindFirstObjectByType<LaundryStaff>(FindObjectsInactive.Include);
            var folding = Object.FindFirstObjectByType<FoldingStation>(FindObjectsInactive.Include);
            var counter = FindByName("Counter");

            if (spawner == null || staff == null || folding == null)
            {
                Debug.LogError($"[BUSINESS] spawner={spawner != null} staff={staff != null} folding={folding != null}");
                return;
            }

            var oldSystems = GameObject.Find("BusinessSystems");
            if (oldSystems != null)
            {
                Object.DestroyImmediate(oldSystems);
            }

            Transform counterTransform = counter != null ? counter.transform : null;
            var rack = BuildOrderRack(counterTransform);

            var root = new GameObject("BusinessSystems");
            SceneManager.MoveGameObjectToScene(root, scene);

            var clock = root.AddComponent<BusinessClock>();
            clock.Configure(1, 480, 1f);

            var stock = root.AddComponent<ConsumableStockService>();
            var shelves = new List<ConsumableShelf>
            {
                BuildShelf(root.transform, spawner, "Shelf_Detergent", new Vector3(7.35f, 0.55f, -3.5f),
                    EnsureConsumable("detergent", "Detergent", ConsumableType.Detergent, 2),
                    new Color(0.2f, 0.45f, 0.9f)),
                BuildShelf(root.transform, spawner, "Shelf_Fragrance", new Vector3(7.35f, 0.55f, -2.0f),
                    EnsureConsumable("fragrance", "Fragrance", ConsumableType.Fragrance, 3),
                    new Color(0.7f, 0.35f, 0.9f)),
                BuildShelf(root.transform, spawner, "Shelf_Plastic", new Vector3(7.35f, 0.55f, -0.5f),
                    EnsureConsumable("plastic", "Plastic", ConsumableType.Plastic, 1),
                    new Color(0.85f, 0.9f, 0.95f))
            };
            stock.Configure(shelves);

            var billing = root.AddComponent<BillingService>();
            billing.Configure(clock, spawner);
            billing.ConfigureAmounts(250, 90, 60, 30, 3, 0.1f);

            var businessPanel = root.AddComponent<BusinessPanelView>();
            businessPanel.Configure(clock, stock, spawner, billing, rack);
            var billsPanel = root.AddComponent<BillsPanelView>();
            billsPanel.Configure(billing);

            spawner.SetOrderRack(rack);
            spawner.SetMaxCustomers(OrderSlots);
            spawner.SetRespawnDelay(10f);
            staff.SetOrderRack(rack);

            foreach (MachineLoadHook hook in Object.FindObjectsByType<MachineLoadHook>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                hook.SetStockService(stock);
            }

            folding.SetStockService(stock);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BUSINESS] Built 3 shelves, a {OrderSlots}-slot rack, clock, billing and HUD.");
        }

        [MenuItem("Laundry Legends/Phase 10/Validate Business Systems")]
        public static void ValidateBusinessSystems()
        {
            var failures = new List<string>();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            var staff = Object.FindFirstObjectByType<LaundryStaff>(FindObjectsInactive.Include);
            var stock = Object.FindFirstObjectByType<ConsumableStockService>(FindObjectsInactive.Include);
            var rack = Object.FindFirstObjectByType<DropOffRack>(FindObjectsInactive.Include);
            var clock = Object.FindFirstObjectByType<BusinessClock>(FindObjectsInactive.Include);
            var billing = Object.FindFirstObjectByType<BillingService>(FindObjectsInactive.Include);
            var panel = Object.FindFirstObjectByType<BusinessPanelView>(FindObjectsInactive.Include);
            var bills = Object.FindFirstObjectByType<BillsPanelView>(FindObjectsInactive.Include);

            if (spawner == null) failures.Add("CustomerSpawner missing");
            if (staff == null) failures.Add("LaundryStaff missing");
            if (stock == null) failures.Add("ConsumableStockService missing");
            if (rack == null) failures.Add("DropOffRack missing");
            if (clock == null) failures.Add("BusinessClock missing");
            if (billing == null) failures.Add("BillingService missing");
            if (panel == null) failures.Add("BusinessPanelView missing");
            if (bills == null) failures.Add("BillsPanelView missing");
            if (rack != null && rack.Capacity != OrderSlots) failures.Add($"Order rack has {rack.Capacity} slots");
            if (spawner != null && spawner.MaxCustomers != OrderSlots) failures.Add($"Spawner max is {spawner.MaxCustomers}");
            if (stock != null && stock.Shelves.Count != 3) failures.Add($"Stock service has {stock.Shelves.Count} shelves");
            if (stock != null
                && (stock.Find(ConsumableType.Detergent) == null
                    || stock.Find(ConsumableType.Fragrance) == null
                    || stock.Find(ConsumableType.Plastic) == null))
            {
                failures.Add("One or more consumable types are missing a shelf");
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[BUSINESS] {failure}");
                }

                throw new System.InvalidOperationException($"Business validation failed: {string.Join("; ", failures)}");
            }

            Debug.Log($"[BUSINESS] Validation passed: {rack.Capacity} order slots, 3 shelves, clock, billing, HUD and bills menu.");
        }

        private static DropOffRack BuildOrderRack(Transform counter)
        {
            GameObject rackRoot = FindByName("OrderRack");
            if (rackRoot != null)
            {
                Object.DestroyImmediate(rackRoot);
            }

            rackRoot = new GameObject("OrderRack");
            if (counter != null)
            {
                rackRoot.transform.position = counter.position;
            }

            var rack = rackRoot.AddComponent<DropOffRack>();
            var slots = new List<CarrySlot>();
            for (int i = 0; i < OrderSlots; i++)
            {
                var slotObject = new GameObject($"OrderSlot_{i:00}");
                slotObject.transform.SetParent(rackRoot.transform, true);
                Vector3 world = counter != null
                    ? counter.position + counter.right * ((i - (OrderSlots - 1) * 0.5f) * 0.28f) + Vector3.up * 0.75f
                    : new Vector3(-4.5f + (i - (OrderSlots - 1) * 0.5f) * 0.28f, 1.3f, -2.5f);
                slotObject.transform.position = world;
                slotObject.transform.rotation = counter != null ? counter.rotation : Quaternion.identity;
                slotObject.transform.localScale = Vector3.one;

                var slot = slotObject.AddComponent<CarrySlot>();
                slot.AcceptAllKinds();
                SetAnchor(slot, slotObject.transform);
                var marker = CreateBox(slotObject.transform, "Marker", Vector3.zero,
                    new Vector3(0.18f, 0.02f, 0.18f), EnsureMaterial($"{MaterialFolder}/MAT_OrderSlot.mat",
                        "MAT_OrderSlot", new Color(0.08f, 0.09f, 0.12f)));
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                slots.Add(slot);
            }

            rack.Configure(slots);

            var oldDrop = FindByName("DropOffSlot");
            if (oldDrop != null)
            {
                oldDrop.SetActive(false);
            }

            return rack;
        }

        private static ConsumableShelf BuildShelf(
            Transform parent,
            CustomerSpawner spawner,
            string name,
            Vector3 position,
            ConsumableDefinition definition,
            Color unitColor)
        {
            GameObject shelf = CreateBox(parent, name, position, new Vector3(0.5f, 1.1f, 1.0f),
                EnsureMaterial($"{MaterialFolder}/MAT_CounterWood.mat", "MAT_CounterWood", new Color(0.45f, 0.28f, 0.16f)));
            var stack = new GameObject("Stack");
            stack.transform.SetParent(shelf.transform, false);
            stack.transform.localPosition = Vector3.zero;

            Material unitMaterial = EnsureMaterial($"{MaterialFolder}/MAT_{name}.mat", $"MAT_{name}", unitColor);
            for (int i = 0; i < 10; i++)
            {
                float x = (i % 5) * 0.12f - 0.24f;
                float z = (i / 5) * 0.32f - 0.16f;
                GameObject unit = CreateBox(stack.transform, $"Unit_{i:00}",
                    new Vector3(x, 0.58f, z), new Vector3(0.1f, 0.22f, 0.1f), unitMaterial);
                Object.DestroyImmediate(unit.GetComponent<Collider>());
            }

            var shelfComponent = shelf.AddComponent<ConsumableShelf>();
            shelfComponent.Configure(definition, spawner, stack.transform, ShelfStock);
            shelfComponent.RefreshVisuals();
            return shelfComponent;
        }

        private static ConsumableDefinition EnsureConsumable(
            string id,
            string displayName,
            ConsumableType type,
            int unitCost)
        {
            Directory.CreateDirectory(DefinitionFolder);
            AssetDatabase.Refresh();
            string path = $"{DefinitionFolder}/Consumable_{displayName}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<ConsumableDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ConsumableDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure($"consumable.{id}", displayName, type, unitCost, 10);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            return definition;
        }

        private static Material EnsureMaterial(string path, string name, Color color)
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

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            if (material != null)
            {
                box.GetComponent<Renderer>().sharedMaterial = material;
            }

            return box;
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

        private static GameObject FindByName(string name)
        {
            GameObject target = GameObject.Find(name);
            if (target != null)
            {
                return target;
            }

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }
    }
}
