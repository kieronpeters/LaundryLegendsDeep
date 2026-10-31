using System;
using System.Collections.Generic;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Imports the custom basket FBX and swaps it into the existing Basket
    /// prefab without changing gameplay components or the item contract.
    /// </summary>
    public static class BasketAssetSetupTool
    {
        private const string ModelPath = "Assets/LaundryLegends/Art/Models/LaundryBasket.fbx";
        private const string LooseModelPath = "Assets/LaundryLegends/Art/Models/LooseLaundry.fbx";
        private const string BagModelPath = "Assets/LaundryLegends/Art/Models/Bag.fbx";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string MaterialPath = MaterialFolder + "/MAT_Basket.mat";
        private const string LooseMaterialPath = MaterialFolder + "/MAT_LooseLaundry.mat";
        private const string BagMaterialPath = MaterialFolder + "/MAT_Bag.mat";
        private const string PrefabPath = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/Basket.prefab";
        private const string LoosePrefabPath = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab";
        private const string BagPrefabPath = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/Bag.prefab";
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string FrontDetailsName = "MachineFrontDetails";

        [MenuItem("Laundry Legends/Phase 3/Apply Basket Asset")]
        public static void ApplyBasketAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            {
                throw new InvalidOperationException($"Missing basket model: {ModelPath}");
            }

            ConfigureModelImporter(ModelPath);

            Material material = EnsureMaterial(MaterialPath, "MAT_Basket", new Color(0.42f, 0.28f, 0.14f, 1f), 0.25f);
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                LaundryItem item = prefab.GetComponent<LaundryItem>();
                LaundryItemInteractable pickup = prefab.GetComponent<LaundryItemInteractable>();
                if (item == null || pickup == null)
                {
                    throw new InvalidOperationException("Basket prefab is missing its item or pickup component.");
                }

                ReplaceBody(prefab, ModelPath, material);

                BoxCollider collider = prefab.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = prefab.AddComponent<BoxCollider>();
                }

                collider.center = new Vector3(0f, 0.15f, 0f);
                collider.size = new Vector3(0.5f, 0.3f, 0.35f);

                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Basket] Custom basket imported into Basket.prefab.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Basket Asset")]
        public static void ValidateBasketAsset()
        {
            var failures = new List<string>();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing basket prefab: {PrefabPath}");
            }
            else
            {
                if (prefab.GetComponent<LaundryItem>() == null)
                {
                    failures.Add("Basket prefab lost LaundryItem.");
                }

                if (prefab.GetComponent<LaundryItemInteractable>() == null)
                {
                    failures.Add("Basket prefab lost LaundryItemInteractable.");
                }

                BoxCollider collider = prefab.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    failures.Add("Basket prefab has no BoxCollider.");
                }
                else if ((collider.size - new Vector3(0.5f, 0.3f, 0.35f)).sqrMagnitude > 0.0001f)
                {
                    failures.Add($"Basket collider size changed: {collider.size}.");
                }

                Transform body = prefab.transform.Find("Body");
                if (body == null)
                {
                    failures.Add("Basket prefab has no Body child.");
                }
                else
                {
                    Renderer[] renderers = body.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0)
                    {
                        failures.Add("Basket Body has no renderer.");
                    }

                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.sharedMaterial == null)
                        {
                            failures.Add("Basket renderer has no material.");
                        }
                    }

                    MeshFilter filter = body.GetComponentInChildren<MeshFilter>(true);
                    if (filter == null || filter.sharedMesh == null)
                    {
                        failures.Add("Basket Body has no mesh.");
                    }
                    else
                    {
                        int triangles = filter.sharedMesh.triangles.Length / 3;
                        Debug.Log($"[Basket] Mesh bounds {filter.sharedMesh.bounds.size}, triangles {triangles}.");
                        if (triangles > 2500)
                        {
                            failures.Add($"Basket mesh exceeds the 2,500-triangle budget: {triangles}.");
                        }
                    }
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Basket] {failure}");
                }

                throw new InvalidOperationException($"Basket validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Basket] Validation passed: mesh, material, collider, item, and pickup are intact.");
        }

        [MenuItem("Laundry Legends/Phase 3/Apply Loose and Bag Assets")]
        public static void ApplyLaundryPropAssets()
        {
            ConfigureModelImporter(LooseModelPath);
            ConfigureModelImporter(BagModelPath);

            Material looseMaterial = EnsureMaterial(
                LooseMaterialPath,
                "MAT_LooseLaundry",
                new Color(0.16f, 0.45f, 0.47f, 1f),
                0.2f);
            Material bagMaterial = EnsureMaterial(
                BagMaterialPath,
                "MAT_Bag",
                new Color(0.52f, 0.38f, 0.22f, 1f),
                0.15f);

            ApplyProp(LoosePrefabPath, LooseModelPath, looseMaterial);
            ApplyProp(BagPrefabPath, BagModelPath, bagMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Props] Loose laundry and bag assets applied.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Loose and Bag Assets")]
        public static void ValidateLaundryPropAssets()
        {
            var failures = new List<string>();
            ValidateProp(LoosePrefabPath, LooseModelPath, LaundryItemKind.LooseLaundry, failures);
            ValidateProp(BagPrefabPath, BagModelPath, LaundryItemKind.Bag, failures);

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Props] {failure}");
                }

                throw new InvalidOperationException($"Laundry prop validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Props] Validation passed: loose laundry and bag meshes, materials, colliders, items, and pickup are intact.");
        }

        [MenuItem("Laundry Legends/Phase 3/Apply Machine Fronts")]
        public static void ApplyMachineFronts()
        {
            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform existing = room.transform.Find(FrontDetailsName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var root = new GameObject(FrontDetailsName);
                root.transform.SetParent(room.transform, false);

                Material panelMaterial = LoadMaterial("Grey_Metal");
                Material drawerMaterial = LoadMaterial("Grey_Washer");
                Material slotMaterial = LoadMaterial("Grey_Secret");

                for (int i = 1; i <= 8; i++)
                {
                    Transform machine = FindChild(room.transform, $"Machine_{i}");
                    if (machine == null)
                    {
                        throw new InvalidOperationException($"GreyboxRoom has no Machine_{i} child.");
                    }

                    Vector3 p = machine.localPosition;
                    CreateDetail(root.transform, $"Panel_{i}", new Vector3(p.x, 0.98f, 4.875f), new Vector3(0.30f, 0.16f, 0.04f), panelMaterial);
                    CreateDetail(root.transform, $"Drawer_{i}", new Vector3(p.x, 0.82f, 4.875f), new Vector3(0.22f, 0.07f, 0.04f), drawerMaterial);
                    CreateDetail(root.transform, $"CoinSlot_{i}", new Vector3(p.x + 0.10f, 1.00f, 4.855f), new Vector3(0.05f, 0.025f, 0.02f), slotMaterial);
                }

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Machine] Additive front panels, drawers, and coin slots applied to all eight machines.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Machine Fronts")]
        public static void ValidateMachineFronts()
        {
            var failures = new List<string>();
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (room == null)
            {
                failures.Add($"Missing room prefab: {RoomPrefabPath}");
            }
            else
            {
                Transform details = room.transform.Find(FrontDetailsName);
                if (details == null)
                {
                    failures.Add($"Missing {FrontDetailsName} root.");
                }
                else
                {
                    int expected = 8 * 3;
                    int actual = details.childCount;
                    if (actual != expected)
                    {
                        failures.Add($"Expected {expected} machine front details, found {actual}.");
                    }

                    foreach (Transform detail in details)
                    {
                        if (detail.GetComponent<Collider>() != null)
                        {
                            failures.Add($"{detail.name} must not add a collider.");
                        }

                        if (detail.GetComponent<Renderer>() == null)
                        {
                            failures.Add($"{detail.name} has no renderer.");
                        }
                    }
                }

                MachineLoadHook[] hooks = room.GetComponentsInChildren<MachineLoadHook>(true);
                if (hooks.Length != 1 || hooks[0].gameObject.name != "Machine_1")
                {
                    failures.Add("MachineLoadHook must remain exactly once on Machine_1.");
                }

                Transform machineOne = FindChild(room.transform, "Machine_1");
                if (machineOne == null
                    || (machineOne.localPosition - new Vector3(-3.15f, 0.55f, 5.25f)).sqrMagnitude > 0.0001f
                    || (machineOne.localScale - new Vector3(0.8f, 1.1f, 0.7f)).sqrMagnitude > 0.0001f)
                {
                    failures.Add("Machine_1 geometry changed while adding front details.");
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Machine] {failure}");
                }

                throw new InvalidOperationException($"Machine front validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Machine] Machine front validation passed: eight machines detailed, no colliders, hook and geometry intact.");
        }

        private static void ApplyProp(string prefabPath, string modelPath, Material material)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (prefab.GetComponent<LaundryItem>() == null || prefab.GetComponent<LaundryItemInteractable>() == null)
                {
                    throw new InvalidOperationException($"{prefabPath} is missing its item or pickup component.");
                }

                ReplaceBody(prefab, modelPath, material);
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void ValidateProp(
            string prefabPath,
            string modelPath,
            LaundryItemKind expectedKind,
            List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing prefab: {prefabPath}");
                return;
            }

            LaundryItem item = prefab.GetComponent<LaundryItem>();
            if (item == null || item.Kind != expectedKind)
            {
                failures.Add($"{prefabPath} has the wrong item kind.");
            }

            if (prefab.GetComponent<LaundryItemInteractable>() == null)
            {
                failures.Add($"{prefabPath} lost its pickup component.");
            }

            if (prefab.GetComponent<Collider>() == null)
            {
                failures.Add($"{prefabPath} lost its collider.");
            }

            Transform body = prefab.transform.Find("Body");
            if (body == null)
            {
                failures.Add($"{prefabPath} has no Body child.");
                return;
            }

            if (body.GetComponentInChildren<Renderer>(true) == null)
            {
                failures.Add($"{prefabPath} Body has no renderer.");
            }

            MeshFilter filter = body.GetComponentInChildren<MeshFilter>(true);
            if (filter == null || filter.sharedMesh == null)
            {
                failures.Add($"{prefabPath} Body has no mesh.");
            }
            else
            {
                int triangles = filter.sharedMesh.triangles.Length / 3;
                Debug.Log($"[Props] {prefabPath} bounds {filter.sharedMesh.bounds.size}, triangles {triangles}.");
                if (triangles > 2500)
                {
                    failures.Add($"{prefabPath} exceeds the triangle budget: {triangles}.");
                }
            }
        }

        private static void ReplaceBody(GameObject prefab, string modelPath, Material material)
        {
            Transform oldBody = prefab.transform.Find("Body");
            if (oldBody != null)
            {
                UnityEngine.Object.DestroyImmediate(oldBody.gameObject);
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                throw new InvalidOperationException($"Missing model: {modelPath}");
            }

            var body = (GameObject)PrefabUtility.InstantiatePrefab(model, prefab.transform);
            body.name = "Body";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;

            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateDetail(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            detail.name = name;
            UnityEngine.Object.DestroyImmediate(detail.GetComponent<Collider>());
            detail.transform.SetParent(parent, false);
            detail.transform.localPosition = position;
            detail.transform.localRotation = Quaternion.identity;
            detail.transform.localScale = size;
            detail.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material LoadMaterial(string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing material: {path}");
            }

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

        private static void ConfigureModelImporter(string modelPath)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"No model importer for {modelPath}");
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.SaveAndReimport();
        }

        private static Material EnsureMaterial(string materialPath, string name, Color color, float glossiness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                return material;
            }

            if (!AssetDatabase.IsValidFolder("Assets/LaundryLegends/Art"))
            {
                AssetDatabase.CreateFolder("Assets/LaundryLegends", "Art");
            }

            if (!AssetDatabase.IsValidFolder("Assets/LaundryLegends/Art/Models"))
            {
                AssetDatabase.CreateFolder("Assets/LaundryLegends/Art", "Models");
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/LaundryLegends/Art", "Materials");
            }

            Shader shader = Shader.Find("Standard");
            material = new Material(shader)
            {
                name = name,
                color = color
            };
            material.SetFloat("_Glossiness", glossiness);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }
    }
}
