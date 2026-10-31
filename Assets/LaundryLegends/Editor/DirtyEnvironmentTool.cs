using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Adds non-colliding grime, dirt piles, a mop, and a bucket so the room
    /// reads as a working laundromat rather than a clean greybox.
    /// </summary>
    public static class DirtyEnvironmentTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string RootName = "GrimeAndDebris";

        [MenuItem("Laundry Legends/Phase 3/Build Grime and Debris")]
        public static void BuildGrimeAndDebris()
        {
            Material grime = EnsureMaterial("MAT_Grime", new Color(0.16f, 0.13f, 0.10f, 1f), 0.85f);
            Material dirt = EnsureMaterial("MAT_Dirt", new Color(0.22f, 0.17f, 0.12f, 1f), 0.95f);
            Material mop = EnsureMaterial("MAT_Mop", new Color(0.42f, 0.38f, 0.30f, 1f), 0.9f);
            Material metal = LoadMaterial("Grey_Metal");

            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform old = room.transform.Find(RootName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var root = new GameObject(RootName);
                root.transform.SetParent(room.transform, false);

                AddSphere(root.transform, "Dirt_Entrance", new Vector3(-1.7f, 0.02f, -4.6f), new Vector3(0.5f, 0.05f, 0.35f), dirt);
                AddSphere(root.transform, "Dirt_MachineWall", new Vector3(2.2f, 0.02f, 4.6f), new Vector3(0.7f, 0.06f, 0.4f), dirt);
                AddSphere(root.transform, "Dirt_Counter", new Vector3(-3.4f, 0.02f, -2.0f), new Vector3(0.35f, 0.04f, 0.3f), dirt);

                AddCube(root.transform, "MopHandle", new Vector3(-6.4f, 0.75f, 3.7f), new Vector3(0.035f, 1.5f, 0.035f), mop);
                AddCube(root.transform, "MopHead", new Vector3(-6.4f, 0.08f, 3.7f), new Vector3(0.35f, 0.08f, 0.18f), mop);
                AddCylinder(root.transform, "Bucket", new Vector3(-6.0f, 0.2f, 3.35f), new Vector3(0.32f, 0.2f, 0.32f), metal);

                for (int i = 0; i < 5; i++)
                {
                    float x = -5.5f + i * 2.6f;
                    AddCube(root.transform, $"WallScuff_{i}", new Vector3(x, 0.95f, 5.62f), new Vector3(0.45f, 0.08f, 0.02f), grime);
                }

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
                Debug.Log("[Grime] Added dirt piles, mop, bucket, and wall scuffs.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Grime and Debris")]
        public static void ValidateGrimeAndDebris()
        {
            var failures = new List<string>();
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            Transform root = room != null ? room.transform.Find(RootName) : null;
            if (root == null)
            {
                failures.Add("Missing GrimeAndDebris root.");
            }
            else
            {
                int dirt = 0;
                foreach (Transform child in root)
                {
                    if (child.name.StartsWith("Dirt_"))
                    {
                        dirt++;
                    }

                    if (child.GetComponent<Collider>() != null)
                    {
                        failures.Add($"{child.name} must not add a collider.");
                    }
                }

                if (dirt < 3)
                {
                    failures.Add("Expected at least 3 dirt piles.");
                }

                if (root.Find("MopHandle") == null || root.Find("Bucket") == null)
                {
                    failures.Add("Mop or bucket missing.");
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Grime] {failure}");
                }

                throw new System.InvalidOperationException($"Grime/debris validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Grime] Validation passed: dirt, mop, bucket, and scuffs are non-colliding.");
        }

        private static void AddCube(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AddSphere(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = position;
            sphere.transform.localScale = size;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AddCylinder(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = size;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material EnsureMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material LoadMaterial(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
            if (material == null)
            {
                throw new System.InvalidOperationException($"Missing material: {name}");
            }

            return material;
        }
    }
}
