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
    /// Adds the Phase 2 physical-environment pass to the existing greybox room:
    /// a storefront entrance, south-wall windows, and a minimal outside backdrop.
    /// The tool is additive and idempotent; it never rebuilds unrelated room content.
    /// </summary>
    public static class Phase2SetupTool
    {
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string LaundromatScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        private static readonly string[] GeneratedChildNames =
        {
            "Wall_South_Left_PierA",
            "Wall_South_Left_Sill",
            "Wall_South_Left_Header",
            "Wall_South_Left_PierB",
            "Wall_South_Right_PierA",
            "Wall_South_Right_Sill",
            "Wall_South_Right_Header",
            "Wall_South_Right_PierB",
            "Entrance_Jamb_Left",
            "Entrance_Jamb_Right",
            "Entrance_Header",
            "Entrance_Threshold",
            "Entrance_Door_Left",
            "Entrance_Door_Right",
            "Window_Left_FrameLeft",
            "Window_Left_FrameRight",
            "Window_Left_FrameTop",
            "Window_Left_FrameBottom",
            "Window_Left_Mullion",
            "Window_Left_Glass",
            "Window_Right_FrameLeft",
            "Window_Right_FrameRight",
            "Window_Right_FrameTop",
            "Window_Right_FrameBottom",
            "Window_Right_Mullion",
            "Window_Right_Glass",
            "Facade_Backdrop",
            "Facade_Ground"
        };

        [MenuItem("Laundry Legends/Phase 2/Build Entrance Envelope")]
        public static void BuildEntranceEnvelope()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath) == null)
            {
                throw new InvalidOperationException($"Missing room prefab: {RoomPrefabPath}");
            }

            Material wall = RequireMaterial("Grey_Wall");
            Material metal = RequireMaterial("Grey_Metal");
            Material floor = RequireMaterial("Grey_Floor");
            Material glass = GetOrCreateGlassMaterial();
            Material sky = GetOrCreateSkyMaterial();

            GameObject contents = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform root = contents.transform;
                RemoveGeneratedChildren(root);
                RemoveChild(root, "Wall_South_Left");
                RemoveChild(root, "Wall_South_Right");

                BuildSouthWalls(root, wall);
                BuildEntrance(root, metal, glass);
                BuildWindow(root, -5f, "Window_Left", metal, glass);
                BuildWindow(root, 5f, "Window_Right", metal, glass);
                BuildExterior(root, floor, sky);

                PrefabUtility.SaveAsPrefabAsset(contents, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            ConfigureSceneExterior();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2] Entrance envelope built into the greybox room prefab.");
        }

        [MenuItem("Laundry Legends/Phase 2/Validate Entrance Envelope")]
        public static void ValidateEntranceEnvelope()
        {
            var failures = new List<string>();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing room prefab: {RoomPrefabPath}");
            }
            else
            {
                ValidatePrefabStructure(prefab, failures);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                GameObject room = FindRoot(scene, "GreyboxRoom");
                if (room == null)
                {
                    failures.Add("Laundromat has no GreyboxRoom instance.");
                }
                else
                {
                    Physics.SyncTransforms();
                    ValidateSceneInstance(room, failures);
                    ValidateEntranceRoute(failures);
                    ValidateSecretDoorStillClosed(room, failures);
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
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Phase2] {failure}");
                }

                throw new InvalidOperationException($"Entrance envelope validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Phase2] Validation passed: entrance, windows, exterior backdrop, collision, and existing room content are present.");
        }

        [MenuItem("Laundry Legends/Phase 2/Capture Entrance Envelope")]
        public static void CaptureEntranceEnvelope()
        {
            EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            // View from inside the room looking back toward the storefront.
            CaptureView("phase2-entrance.png", new Vector3(0f, 1.7f, -1.2f), new Vector3(0f, 1.25f, -6f));
            // View across the room toward the south windows.
            CaptureView("phase2-storefront.png", new Vector3(0f, 1.7f, 3.2f), new Vector3(-5f, 1.6f, -6f));

            Debug.Log("[Phase2] Captured entrance/storefront previews under TestResults/.");
        }

        private static void BuildSouthWalls(Transform root, Material wall)
        {
            // Left segment: x -8..-2, window opening at x -6.2..-3.8, y 0.9..2.4.
            CreateBox("Wall_South_Left_PierA", root, new Vector3(-7.1f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);
            CreateBox("Wall_South_Left_Sill", root, new Vector3(-5f, 0.45f, -6f), new Vector3(2.4f, 0.9f, 0.2f), wall);
            CreateBox("Wall_South_Left_Header", root, new Vector3(-5f, 2.8f, -6f), new Vector3(2.4f, 0.8f, 0.2f), wall);
            CreateBox("Wall_South_Left_PierB", root, new Vector3(-2.9f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);

            // Right segment: x 2..8, window opening at x 3.8..6.2, y 0.9..2.4.
            CreateBox("Wall_South_Right_PierA", root, new Vector3(2.9f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);
            CreateBox("Wall_South_Right_Sill", root, new Vector3(5f, 0.45f, -6f), new Vector3(2.4f, 0.9f, 0.2f), wall);
            CreateBox("Wall_South_Right_Header", root, new Vector3(5f, 2.8f, -6f), new Vector3(2.4f, 0.8f, 0.2f), wall);
            CreateBox("Wall_South_Right_PierB", root, new Vector3(7.1f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);
        }

        private static void BuildEntrance(Transform root, Material metal, Material glass)
        {
            CreateBox("Entrance_Jamb_Left", root, new Vector3(-2.1f, 1.2f, -5.95f), new Vector3(0.2f, 2.4f, 0.25f), metal);
            CreateBox("Entrance_Jamb_Right", root, new Vector3(2.1f, 1.2f, -5.95f), new Vector3(0.2f, 2.4f, 0.25f), metal);
            CreateBox("Entrance_Header", root, new Vector3(0f, 2.5f, -5.95f), new Vector3(4.4f, 0.2f, 0.25f), metal);
            CreateBox("Entrance_Threshold", root, new Vector3(0f, 0.025f, -5.95f), new Vector3(4.2f, 0.05f, 0.35f), metal);

            // Open glass leaves rest against the interior side of each jamb.
            CreateBox("Entrance_Door_Left", root, new Vector3(-2.05f, 1.15f, -4.95f), new Vector3(0.06f, 2.3f, 1.9f), glass);
            CreateBox("Entrance_Door_Right", root, new Vector3(2.05f, 1.15f, -4.95f), new Vector3(0.06f, 2.3f, 1.9f), glass);
        }

        private static void BuildWindow(Transform root, float centerX, string prefix, Material metal, Material glass)
        {
            CreateBox(prefix + "_FrameLeft", root, new Vector3(centerX - 1.24f, 1.65f, -5.94f), new Vector3(0.08f, 1.6f, 0.12f), metal);
            CreateBox(prefix + "_FrameRight", root, new Vector3(centerX + 1.24f, 1.65f, -5.94f), new Vector3(0.08f, 1.6f, 0.12f), metal);
            CreateBox(prefix + "_FrameTop", root, new Vector3(centerX, 2.42f, -5.94f), new Vector3(2.56f, 0.08f, 0.12f), metal);
            CreateBox(prefix + "_FrameBottom", root, new Vector3(centerX, 0.88f, -5.94f), new Vector3(2.56f, 0.08f, 0.12f), metal);
            CreateBox(prefix + "_Mullion", root, new Vector3(centerX, 1.65f, -5.94f), new Vector3(0.06f, 1.5f, 0.1f), metal);
            CreateBox(prefix + "_Glass", root, new Vector3(centerX, 1.65f, -5.98f), new Vector3(2.44f, 1.5f, 0.04f), glass);
        }

        private static void BuildExterior(Transform root, Material floor, Material sky)
        {
            CreateBox("Facade_Ground", root, new Vector3(0f, -0.3f, -7.35f), new Vector3(20f, 0.6f, 2.5f), floor);
            CreateBox("Facade_Backdrop", root, new Vector3(0f, 2.2f, -8.7f), new Vector3(20f, 5f, 0.2f), sky);
        }

        private static void ConfigureSceneExterior()
        {
            Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            // Keep the existing ambient settings; the new facade adds no scene-level lights.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.25f, 0.28f);
            RenderSettings.fog = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidatePrefabStructure(GameObject prefab, List<string> failures)
        {
            RequireChild(prefab, "Floor", failures);
            RequireChild(prefab, "Ceiling", failures);
            RequireChild(prefab, "MachineBackPanel", failures);
            RequireChild(prefab, "SecretDoor_Foreshadowed", failures);
            RequireChild(prefab, "Entrance_Jamb_Left", failures);
            RequireChild(prefab, "Entrance_Jamb_Right", failures);
            RequireChild(prefab, "Entrance_Header", failures);
            RequireChild(prefab, "Entrance_Threshold", failures);
            RequireChild(prefab, "Window_Left_Glass", failures);
            RequireChild(prefab, "Window_Right_Glass", failures);
            RequireChild(prefab, "Facade_Backdrop", failures);
            RequireChild(prefab, "Facade_Ground", failures);

            if (prefab.transform.Find("Wall_South_Left") != null || prefab.transform.Find("Wall_South_Right") != null)
            {
                failures.Add("Original solid south wall segments were not replaced.");
            }

            if (prefab.GetComponentsInChildren<Collider>(true).Length < 30)
            {
                failures.Add("Prefab should have at least 30 colliders after the facade pass.");
            }

            Transform threshold = prefab.transform.Find("Entrance_Threshold");
            if (threshold != null && threshold.localScale.y > 0.1f)
            {
                failures.Add($"Entrance threshold is too tall: {threshold.localScale.y:0.00} m.");
            }
        }

        private static void ValidateSceneInstance(GameObject room, List<string> failures)
        {
            RequireChild(room, "Entrance_Threshold", failures);
            RequireChild(room, "Window_Left_Glass", failures);
            RequireChild(room, "Window_Right_Glass", failures);
            RequireChild(room, "Facade_Backdrop", failures);
            RequireChild(room, "Facade_Ground", failures);
        }

        private static void ValidateEntranceRoute(List<string> failures)
        {
            // The center of the doorway must stay open all the way to the exterior.
            // Start inside the opening, past the player spawn capsule.
            if (Physics.Raycast(new Vector3(0f, 1.2f, -5.0f), Vector3.back, out RaycastHit hit, 1.85f))
            {
                failures.Add($"Doorway center is blocked by {hit.collider.gameObject.name} at {hit.distance:0.00} m.");
            }

            CheckWindowRay(failures, -5f, "Window_Left_Glass");
            CheckWindowRay(failures, 5f, "Window_Right_Glass");
        }

        private static void CheckWindowRay(List<string> failures, float centerX, string expectedName)
        {
            // Offset from the center mullion so the ray tests the glass pane.
            float sampleX = centerX - 0.8f;
            if (!Physics.Raycast(new Vector3(sampleX, 1.65f, -4.5f), Vector3.back, out RaycastHit hit, 1.6f))
            {
                failures.Add($"No window collision found at x={sampleX:0.0}.");
                return;
            }

            if (hit.collider.gameObject.name != expectedName)
            {
                failures.Add($"Window ray at x={sampleX:0.0} hit {hit.collider.gameObject.name}, expected {expectedName}.");
            }
        }

        private static void ValidateSecretDoorStillClosed(GameObject room, List<string> failures)
        {
            Transform door = room.transform.Find("SecretDoor_Foreshadowed");
            if (door == null)
            {
                failures.Add("SecretDoor_Foreshadowed is missing after the facade pass.");
                return;
            }

            Collider collider = door.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
            {
                failures.Add("SecretDoor_Foreshadowed collider must remain enabled after the facade pass.");
            }
        }

        private static void RequireChild(GameObject parent, string name, List<string> failures)
        {
            if (parent.transform.Find(name) == null)
            {
                failures.Add($"Missing expected object: {name}.");
            }
        }

        private static void RemoveGeneratedChildren(Transform root)
        {
            foreach (string name in GeneratedChildNames)
            {
                RemoveChild(root, name);
            }
        }

        private static void RemoveChild(Transform root, string name)
        {
            Transform child = root.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
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

        private static Material RequireMaterial(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
            if (material == null)
            {
                throw new InvalidOperationException($"Missing greybox material: {name}.");
            }

            return material;
        }

        private static Material GetOrCreateGlassMaterial()
        {
            string path = $"{MaterialFolder}/Grey_Glass.mat";
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

            material.SetOverrideTag("RenderType", "Transparent");
            material.color = new Color(0.55f, 0.75f, 0.85f, 0.45f);
            material.SetFloat("_Glossiness", 0.85f);
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateSkyMaterial()
        {
            string path = $"{MaterialFolder}/Grey_Sky.mat";
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

            material.color = new Color(0.58f, 0.72f, 0.84f);
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.18f, 0.25f, 0.33f));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CaptureView(string fileName, Vector3 position, Vector3 lookAt)
        {
            var cameraObject = new GameObject("Phase2PreviewCamera");
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

            string directory = Path.GetFullPath("TestResults");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
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
    }
}
