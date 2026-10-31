using System;
using System.Collections.Generic;
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
        private const string ItemsRootName = "Phase3Items";

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

            ConfigureSceneItems();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3] Placeholder laundry prefabs and additive scene placement built.");
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
