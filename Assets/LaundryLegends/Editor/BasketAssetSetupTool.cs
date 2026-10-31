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
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string MaterialPath = MaterialFolder + "/MAT_Basket.mat";
        private const string PrefabPath = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/Basket.prefab";

        [MenuItem("Laundry Legends/Phase 3/Apply Basket Asset")]
        public static void ApplyBasketAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            {
                throw new InvalidOperationException($"Missing basket model: {ModelPath}");
            }

            ConfigureModelImporter();

            Material material = EnsureMaterial();
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                LaundryItem item = prefab.GetComponent<LaundryItem>();
                LaundryItemInteractable pickup = prefab.GetComponent<LaundryItemInteractable>();
                if (item == null || pickup == null)
                {
                    throw new InvalidOperationException("Basket prefab is missing its item or pickup component.");
                }

                Transform oldBody = prefab.transform.Find("Body");
                if (oldBody != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldBody.gameObject);
                }

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var body = (GameObject)PrefabUtility.InstantiatePrefab(model, prefab.transform);
                body.name = "Body";
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale = Vector3.one;

                foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }

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

        private static void ConfigureModelImporter()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"No model importer for {ModelPath}");
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

        private static Material EnsureMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
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
                name = "MAT_Basket",
                color = new Color(0.42f, 0.28f, 0.14f, 1f)
            };
            material.SetFloat("_Glossiness", 0.25f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
