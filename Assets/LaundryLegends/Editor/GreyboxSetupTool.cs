using System;
using System.Collections.Generic;
using System.IO;
using LaundryLegends.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the greybox laundromat as a reproducible prefab and places it in the scene.
    /// Geometry is deliberately simple: boxes, cylinders, standard materials, and point lights.
    /// </summary>
    public static class GreyboxSetupTool
    {
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string EnvironmentFolder = "Assets/LaundryLegends/Prefabs/Environment";
        private const string RoomPrefabPath = EnvironmentFolder + "/GreyboxRoom.prefab";
        private const string LaundromatScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Build Greybox Room")]
        public static void BuildGreyboxRoom()
        {
            EnsureFolders();
            GreyboxMaterials materials = CreateMaterials();
            GameObject room = BuildRoomObject(materials);
            PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
            UnityEngine.Object.DestroyImmediate(room);

            ConfigureLaundromatScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Greybox] Room prefab built and placed in Laundromat.");
        }

        [MenuItem("Laundry Legends/Validate Greybox Room")]
        public static void ValidateGreyboxRoom()
        {
            var failures = new List<string>();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing greybox prefab: {RoomPrefabPath}");
            }
            else
            {
                ValidateRoomObject(prefab, "Prefab", failures);
            }

            Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
            GameObject room = FindRoot(scene, "GreyboxRoom");
            if (room == null)
            {
                failures.Add("Laundromat has no GreyboxRoom instance.");
            }
            else
            {
                Physics.SyncTransforms();
                ValidateFloorSupport(failures);
                ValidateWalls(failures);
                ValidateSecretDoor(room, failures);
                ValidateLighting(room, failures);
            }

            FirstPersonController player = FindPlayer(scene);
            if (player == null)
            {
                failures.Add("Laundromat has no player.");
            }
            else if (player.transform.position.y < -0.1f)
            {
                failures.Add($"Player spawn is below the floor: y={player.transform.position.y:0.00}.");
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Greybox] {failure}");
                }

                throw new InvalidOperationException($"Greybox validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Greybox] Validation passed: floor support, wall containment, secret door collider, lighting, and player spawn are present.");
        }

        [MenuItem("Laundry Legends/Capture Greybox Preview")]
        public static void CaptureGreyboxPreview()
        {
            EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            CaptureView("greybox-entrance.png", new Vector3(0f, 1.75f, -5.4f), new Vector3(0f, 1.0f, 4.5f));
            CaptureView("greybox-secret.png", new Vector3(4.5f, 1.7f, 2.8f), new Vector3(7.8f, 1.1f, 3f));

            Debug.Log("[Greybox] Captured preview images under TestResults/.");
        }

        private static void CaptureView(string fileName, Vector3 position, Vector3 lookAt)
        {
            var cameraObject = new GameObject("GreyboxPreviewCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 65f;
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

            string directory = Path.GetFullPath("TestResults");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ValidateRoomObject(GameObject room, string label, List<string> failures)
        {
            if (room.transform.Find("Floor") == null)
            {
                failures.Add($"{label}: missing Floor.");
            }

            if (room.transform.Find("Ceiling") == null)
            {
                failures.Add($"{label}: missing Ceiling.");
            }

            if (room.transform.Find("MachineBackPanel") == null)
            {
                failures.Add($"{label}: missing MachineBackPanel.");
            }

            if (room.transform.Find("SecretDoor_Foreshadowed") == null)
            {
                failures.Add($"{label}: missing SecretDoor_Foreshadowed.");
            }

            if (room.GetComponentsInChildren<Collider>(true).Length < 20)
            {
                failures.Add($"{label}: expected at least 20 colliders.");
            }
        }

        private static void ValidateFloorSupport(List<string> failures)
        {
            Vector2[] points =
            {
                new Vector2(0f, 0f),
                new Vector2(5f, 0f),
                new Vector2(-5f, 0f),
                new Vector2(0f, 3f),
                new Vector2(5f, 3f),
                new Vector2(-5f, 3f),
                new Vector2(0f, 4.5f),
                new Vector2(-6f, -4f)
            };

            foreach (Vector2 point in points)
            {
                if (!Physics.Raycast(new Vector3(point.x, 2f, point.y), Vector3.down, out RaycastHit hit, 5f))
                {
                    failures.Add($"No floor collider at ({point.x:0.0}, {point.y:0.0}).");
                }
                else if (hit.point.y < -0.5f || hit.point.y > 2.2f)
                {
                    failures.Add($"Unexpected floor height at ({point.x:0.0}, {point.y:0.0}): {hit.point.y:0.00}.");
                }
            }
        }

        private static void ValidateWalls(List<string> failures)
        {
            CheckRay(failures, "east wall", new Vector3(0f, 1.2f, 0f), Vector3.right, 9f);
            CheckRay(failures, "west wall", new Vector3(0f, 1.2f, 0f), Vector3.left, 9f);
            CheckRay(failures, "north machine wall", new Vector3(0f, 1.2f, 0f), Vector3.forward, 6.2f);
            CheckRay(failures, "south wall left", new Vector3(-5f, 1.2f, 0f), Vector3.back, 6.5f);
            CheckRay(failures, "south wall right", new Vector3(5f, 1.2f, 0f), Vector3.back, 6.5f);
        }

        private static void CheckRay(List<string> failures, string label, Vector3 origin, Vector3 direction, float maxDistance)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
            {
                failures.Add($"No collision found for {label}.");
            }
            else if (hit.distance > maxDistance)
            {
                failures.Add($"{label} collision is too far: {hit.distance:0.00} m.");
            }
        }

        private static void ValidateSecretDoor(GameObject room, List<string> failures)
        {
            Transform door = room.transform.Find("SecretDoor_Foreshadowed");
            if (door == null)
            {
                failures.Add("SecretDoor_Foreshadowed is missing.");
                return;
            }

            Collider collider = door.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
            {
                failures.Add("SecretDoor_Foreshadowed must have an enabled collider and remain closed.");
            }

            CheckRay(failures, "secret door panel", new Vector3(6.5f, 1.2f, 3f), Vector3.right, 2f);
        }

        private static void ValidateLighting(GameObject room, List<string> failures)
        {
            Light[] lights = room.GetComponentsInChildren<Light>(true);
            int pointLights = 0;
            foreach (Light light in lights)
            {
                if (light.type == LightType.Point)
                {
                    pointLights++;
                }
            }

            if (pointLights < 4)
            {
                failures.Add($"Expected at least four ceiling point lights; found {pointLights}.");
            }
        }

        private static GreyboxMaterials CreateMaterials()
        {
            return new GreyboxMaterials
            {
                Floor = GetOrCreateMaterial("Grey_Floor", new Color(0.30f, 0.32f, 0.35f), 0.05f),
                Wall = GetOrCreateMaterial("Grey_Wall", new Color(0.55f, 0.57f, 0.60f), 0.05f),
                Washer = GetOrCreateMaterial("Grey_Washer", new Color(0.35f, 0.48f, 0.62f), 0.25f),
                Dryer = GetOrCreateMaterial("Grey_Dryer", new Color(0.62f, 0.50f, 0.38f), 0.25f),
                Counter = GetOrCreateMaterial("Grey_Counter", new Color(0.42f, 0.30f, 0.20f), 0.15f),
                Metal = GetOrCreateMaterial("Grey_Metal", new Color(0.30f, 0.32f, 0.35f), 0.45f, 0.6f),
                Secret = GetOrCreateMaterial("Grey_Secret", new Color(0.10f, 0.09f, 0.10f), 0.2f)
            };
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness, float metallic = 0f)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Standard shader is unavailable.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildRoomObject(GreyboxMaterials materials)
        {
            var room = new GameObject("GreyboxRoom");
            Transform root = room.transform;

            CreateBox("Floor", root, new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 12f), materials.Floor);
            CreateBox("Ceiling", root, new Vector3(0f, 3.3f, 0f), new Vector3(16f, 0.2f, 12f), materials.Wall);

            CreateBox("Wall_North", root, new Vector3(0f, 1.6f, 6f), new Vector3(16f, 3.2f, 0.2f), materials.Wall);
            CreateBox("Wall_South_Left", root, new Vector3(-5f, 1.6f, -6f), new Vector3(6f, 3.2f, 0.2f), materials.Wall);
            CreateBox("Wall_South_Right", root, new Vector3(5f, 1.6f, -6f), new Vector3(6f, 3.2f, 0.2f), materials.Wall);
            CreateBox("Wall_South_Header", root, new Vector3(0f, 2.8f, -6f), new Vector3(4f, 0.8f, 0.2f), materials.Wall);
            CreateBox("Wall_East", root, new Vector3(8f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 12f), materials.Wall);
            CreateBox("Wall_West", root, new Vector3(-8f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 12f), materials.Wall);

            CreateBox("Counter", root, new Vector3(-4.5f, 0.55f, -2.5f), new Vector3(3.2f, 1.1f, 0.9f), materials.Counter);
            CreateBox("CounterTop", root, new Vector3(-4.5f, 1.15f, -2.5f), new Vector3(3.4f, 0.1f, 1.1f), materials.Metal);

            CreateBox("FoldingTable", root, new Vector3(4.5f, 0.5f, 1f), new Vector3(3.2f, 1f, 1.2f), materials.Counter);
            CreateBox("FoldingTableTop", root, new Vector3(4.5f, 1.05f, 1f), new Vector3(3.4f, 0.1f, 1.4f), materials.Metal);

            CreateBox("MachineBackPanel", root, new Vector3(0f, 1.5f, 5.75f), new Vector3(8f, 3f, 0.2f), materials.Metal);
            for (int i = 0; i < 8; i++)
            {
                float x = -3.15f + i * 0.9f;
                Material bodyMaterial = i % 2 == 0 ? materials.Washer : materials.Dryer;
                CreateBox($"Machine_{i + 1}", root, new Vector3(x, 0.55f, 5.25f), new Vector3(0.8f, 1.1f, 0.7f), bodyMaterial);
                CreateMachineDoor(root, i, x, materials.Metal);
            }

            CreateBox("SecretDoor_Foreshadowed", root, new Vector3(7.84f, 1.1f, 3f), new Vector3(0.12f, 2.2f, 1.2f), materials.Secret);
            CreateBox("SecretDoor_FrameTop", root, new Vector3(7.80f, 2.25f, 3f), new Vector3(0.18f, 0.12f, 1.5f), materials.Metal);
            CreateBox("SecretDoor_FrameBottom", root, new Vector3(7.80f, 0.02f, 3f), new Vector3(0.18f, 0.12f, 1.5f), materials.Metal);
            CreateBox("SecretDoor_FrameLeft", root, new Vector3(7.80f, 1.1f, 2.25f), new Vector3(0.18f, 2.3f, 0.12f), materials.Metal);
            CreateBox("SecretDoor_FrameRight", root, new Vector3(7.80f, 1.1f, 3.75f), new Vector3(0.18f, 2.3f, 0.12f), materials.Metal);
            CreateBox("SecretDoor_Handle", root, new Vector3(7.70f, 1f, 2.55f), new Vector3(0.08f, 0.08f, 0.18f), materials.Metal);

            CreatePointLight(root, "CeilingLight_1", new Vector3(-4f, 2.9f, -3f), true);
            CreatePointLight(root, "CeilingLight_2", new Vector3(4f, 2.9f, -3f), false);
            CreatePointLight(root, "CeilingLight_3", new Vector3(-4f, 2.9f, 3f), false);
            CreatePointLight(root, "CeilingLight_4", new Vector3(4f, 2.9f, 3f), true);

            return room;
        }

        private static void CreateMachineDoor(Transform parent, int index, float x, Material material)
        {
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            door.name = $"MachineDoor_{index + 1}";
            door.transform.SetParent(parent, false);
            door.transform.localPosition = new Vector3(x, 0.7f, 4.86f);
            door.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            door.transform.localScale = new Vector3(0.55f, 0.03f, 0.55f);
            door.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(door.GetComponent<Collider>());
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 localPosition, bool castShadows)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 2.4f;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static void ConfigureLaundromatScene()
        {
            Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            GameObject existingRoom = FindRoot(scene, "GreyboxRoom");
            if (existingRoom != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoom);
            }

            GameObject temporaryFloor = FindRoot(scene, "TemporaryFloor");
            if (temporaryFloor != null)
            {
                UnityEngine.Object.DestroyImmediate(temporaryFloor);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing greybox prefab: {RoomPrefabPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = Vector3.zero;

            FirstPersonController player = FindPlayer(scene);
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 0.1f, -4.5f);
                player.transform.rotation = Quaternion.identity;
            }

            ConfigureDirectionalLight(scene);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.25f, 0.28f);
            RenderSettings.fog = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureDirectionalLight(Scene scene)
        {
            GameObject lightObject = FindRoot(scene, "Directional Light");
            if (lightObject == null)
            {
                lightObject = new GameObject("Directional Light");
                lightObject.AddComponent<Light>().type = LightType.Directional;
            }

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.55f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static FirstPersonController FindPlayer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FirstPersonController player = root.GetComponentInChildren<FirstPersonController>(true);
                if (player != null)
                {
                    return player;
                }
            }

            return null;
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

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/LaundryLegends/Art", "Materials");
            EnsureFolder("Assets/LaundryLegends/Prefabs", "Environment");
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private sealed class GreyboxMaterials
        {
            public Material Floor;
            public Material Wall;
            public Material Washer;
            public Material Dryer;
            public Material Counter;
            public Material Metal;
            public Material Secret;
        }
    }
}
