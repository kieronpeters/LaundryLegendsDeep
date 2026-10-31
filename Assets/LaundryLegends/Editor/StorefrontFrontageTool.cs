using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Extends the storefront into a walkable sidewalk/road frontage and adds
    /// mounted fascia/counter signage. Additive only; existing entrance geometry
    /// and the fixed-open glass leaves are preserved.
    /// </summary>
    public static class StorefrontFrontageTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string RootName = "FrontageRoot";

        [MenuItem("Laundry Legends/Phase 3/Build Storefront Frontage")]
        public static void BuildFrontage()
        {
            Material asphalt = EnsureMaterial("MAT_Asphalt", new Color(0.16f, 0.17f, 0.18f, 1f), 0.65f);
            Material sidewalk = EnsureMaterial("MAT_Sidewalk", new Color(0.58f, 0.57f, 0.54f, 1f), 0.75f);
            Material sign = LoadMaterial("Grey_Sign");
            Material metal = LoadMaterial("Grey_Metal");
            Material wall = LoadMaterial("Grey_Wall");

            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform old = room.transform.Find(RootName);
                if (old != null)
                {
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }

                var root = new GameObject(RootName);
                root.transform.SetParent(room.transform, false);

                CreateColliderBox(root.transform, "Frontage_Sidewalk", new Vector3(0f, -0.05f, -7.05f), new Vector3(20f, 0.1f, 2.0f), sidewalk);
                CreateColliderBox(root.transform, "Frontage_Road", new Vector3(0f, -0.12f, -11.0f), new Vector3(20f, 0.2f, 6.0f), asphalt);
                CreateColliderBox(root.transform, "Frontage_Curb", new Vector3(0f, 0.04f, -8.1f), new Vector3(20f, 0.16f, 0.18f), metal);

                CreateBox(root.transform, "Frontage_FarWalk", new Vector3(0f, -0.04f, -14.0f), new Vector3(20f, 0.1f, 1.6f), sidewalk);
                CreateBox(root.transform, "Frontage_FarBuilding", new Vector3(0f, 2.2f, -14.9f), new Vector3(20f, 4.4f, 0.3f), wall);

                // Move the old immediate backdrop out of the walkable frontage.
                Transform backdrop = room.transform.Find("Facade_Backdrop");
                if (backdrop != null)
                {
                    backdrop.localPosition = new Vector3(0f, 2.2f, -15.05f);
                    backdrop.localScale = new Vector3(20f, 5f, 0.2f);
                }

                Transform oldStorefrontSign = FindChild(room.transform, "Sign_Storefront");
                if (oldStorefrontSign != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldStorefrontSign.gameObject);
                }

                // Mounted fascia sign above the entrance, readable from the street and from inside.
                CreateBox(root.transform, "Facade_SignPanel", new Vector3(0f, 2.88f, -6.22f), new Vector3(4.6f, 0.62f, 0.1f), sign);
                CreateText(root.transform, "Facade_SignText_Outside", "LAUNDRY LEGENDS", new Vector3(0f, 2.88f, -6.285f), 0.14f, Color.white, 0f);
                CreateText(root.transform, "Facade_HoursText_Outside", "OPEN 7-22", new Vector3(-2.0f, 2.35f, -6.2f), 0.08f, new Color(0.8f, 1f, 0.8f), 0f);

                // Remove legacy floating storefront text so only mounted signs remain.
                foreach (TextMesh legacy in room.GetComponentsInChildren<TextMesh>(true))
                {
                    if (legacy.name.StartsWith("Facade_") || legacy.name.StartsWith("DropOff_")
                        || legacy.name.StartsWith("Counter_") || legacy.name.StartsWith("Blade_"))
                    {
                        continue;
                    }

                    if (legacy.transform.position.z < -5.5f
                        && (legacy.text.Contains("LAUNDRY LEGENDS") || legacy.text.Contains("OPEN") || legacy.text.Contains("HOURS")))
                    {
                        UnityEngine.Object.DestroyImmediate(legacy.gameObject);
                    }
                    else if (legacy.text.Contains("DROP OFF") && legacy.transform.position.z > -5.5f)
                    {
                        UnityEngine.Object.DestroyImmediate(legacy.gameObject);
                    }
                }

                // Mounted drop-off sign above the counter, readable from the room.
                CreateBox(root.transform, "DropOff_Panel", new Vector3(-4.5f, 1.72f, -3.0f), new Vector3(1.7f, 0.38f, 0.06f), sign);
                CreateText(root.transform, "DropOff_Text", "DROP OFF", new Vector3(-4.5f, 1.72f, -3.05f), 0.09f, Color.white, 0f);

                // A small double-sided desk sign on the counter top.
                CreateBox(root.transform, "Counter_DeskSign", new Vector3(-4.5f, 1.26f, -2.35f), new Vector3(0.55f, 0.18f, 0.04f), sign);
                CreateText(root.transform, "Counter_DeskText", "DROP OFF", new Vector3(-4.5f, 1.26f, -2.31f), 0.05f, Color.white, 0f);
                CreateText(root.transform, "Counter_DeskText_Back", "DROP OFF", new Vector3(-4.5f, 1.26f, -2.39f), 0.05f, Color.white, 180f);

                // Blade sign projecting from the facade, readable from the street.
                CreateBox(root.transform, "Blade_Sign", new Vector3(3.55f, 2.15f, -6.35f), new Vector3(1.0f, 0.3f, 0.08f), sign);
                CreateText(root.transform, "Blade_Text", "LAUNDRY", new Vector3(3.55f, 2.15f, -6.40f), 0.07f, Color.white, 0f);

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
                Debug.Log("[Frontage] Walkable sidewalk/road and mounted storefront signage built.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Storefront Frontage")]
        public static void ValidateFrontage()
        {
            var failures = new List<string>();
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (room == null)
            {
                failures.Add($"Missing room prefab: {RoomPrefabPath}");
            }
            else
            {
                Transform root = room.transform.Find(RootName);
                if (root == null)
                {
                    failures.Add($"Missing {RootName}.");
                }
                else
                {
                    Require(root, "Frontage_Sidewalk", failures);
                    Require(root, "Frontage_Road", failures);
                    Require(root, "Facade_SignPanel", failures);
                    Require(root, "Facade_SignText_Outside", failures);
                    Require(root, "DropOff_Panel", failures);
                    Require(root, "Counter_DeskSign", failures);

                    foreach (string name in new[] { "Frontage_Sidewalk", "Frontage_Road", "Frontage_Curb" })
                    {
                        Transform child = root.Find(name);
                        if (child != null && child.GetComponent<Collider>() == null)
                        {
                            failures.Add($"{name} has no collider.");
                        }
                    }
                }

                if (room.transform.Find("Entrance_Door_Left") == null
                    || room.transform.Find("Entrance_Door_Right") == null)
                {
                    failures.Add("Fixed-open entrance doors are missing.");
                }

                Physics.SyncTransforms();
                if (Physics.Linecast(new Vector3(0f, 1.1f, -4.8f), new Vector3(0f, 1.1f, -10.5f), out RaycastHit hit))
                {
                    failures.Add($"Frontage route blocked by {hit.collider.gameObject.name} at {hit.distance:0.00} m.");
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Frontage] {failure}");
                }

                throw new InvalidOperationException($"Frontage validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Frontage] Validation passed: walkable route, colliders, doors, and mounted signs are present.");
        }

        [MenuItem("Laundry Legends/Phase 3/Capture Storefront Frontage")]
        public static void CaptureFrontage()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            CaptureView("frontage-outside.png", new Vector3(0f, 1.7f, -11.5f), new Vector3(0f, 1.6f, -6.0f));
            CaptureView("frontage-entrance.png", new Vector3(0f, 1.7f, -7.3f), new Vector3(0f, 1.5f, -5.8f));
            CaptureView("frontage-dropoff.png", new Vector3(0f, 1.7f, -4.4f), new Vector3(-4.5f, 1.45f, -2.9f));
            Debug.Log("[Frontage] Captured outside/entrance views under TestResults/.");
        }

        private static void CaptureView(string fileName, Vector3 position, Vector3 lookAt)
        {
            var cameraObject = new GameObject("FrontageCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(lookAt);

            var renderTexture = new RenderTexture(1280, 720, 24);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();

            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults"));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void Require(Transform root, string name, List<string> failures)
        {
            if (root.Find(name) == null)
            {
                failures.Add($"Missing {name}.");
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

        private static void CreateBox(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateColliderBox(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateText(Transform parent, string name, string text, Vector3 position, float characterSize, Color color, float yaw)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            TextMesh mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 64;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
        }

        private static Material EnsureMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Standard"))
            {
                name = name,
                color = color
            };
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
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
    }
}
