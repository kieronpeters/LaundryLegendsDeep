using System;
using System.Collections.Generic;
using System.IO;
using LaundryLegends.Interaction;
using LaundryLegends.Player;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Creates the Player prefab and places it in the Laundromat scene with a
    /// temporary floor and light. Re-running keeps existing content.
    /// </summary>
    public static class PlayerSetupTool
    {
        private const string PrefabFolder = "Assets/LaundryLegends/Prefabs/Gameplay";
        private const string PrefabPath = PrefabFolder + "/Player.prefab";
        private const string LaundromatScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Setup Player")]
        public static void SetupPlayer()
        {
            EnsurePrefabFolder();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreatePlayerPrefab();
            }
            else
            {
                Debug.Log($"[PlayerSetup] Keeping existing prefab: {PrefabPath}");
            }

            EnsureCarrySetup();
            EnsurePromptSetup();
            ConfigureLaundromatScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerSetup] Player setup complete.");
        }

        [MenuItem("Laundry Legends/Setup Player Carry")]
        public static void SetupPlayerCarry()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                throw new InvalidOperationException($"Missing player prefab: {PrefabPath}");
            }

            EnsureCarrySetup();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerSetup] Player carry setup complete.");
        }

        [MenuItem("Laundry Legends/Setup Player Prompt")]
        public static void SetupPlayerPrompt()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                throw new InvalidOperationException($"Missing player prefab: {PrefabPath}");
            }

            EnsurePromptSetup();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerSetup] Player prompt setup complete.");
        }

        [MenuItem("Laundry Legends/Validate Player")]
        public static void ValidatePlayerSetup()
        {
            var failures = new List<string>();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing player prefab: {PrefabPath}");
            }
            else
            {
                ValidatePlayerObject(prefab, "Player prefab", failures);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                FirstPersonController player = FindPlayer(scene);
                if (player == null)
                {
                    failures.Add("Laundromat has no FirstPersonController instance.");
                }
                else
                {
                    ValidatePlayerObject(player.gameObject, "Laundromat player", failures);
                }

                if (!HasRoot(scene, "TemporaryFloor") && !HasRoot(scene, "GreyboxRoom"))
                {
                    failures.Add("Laundromat has no walkable floor (TemporaryFloor or GreyboxRoom).");
                }

                if (!HasRoot(scene, "Directional Light"))
                {
                    failures.Add("Laundromat has no Directional Light.");
                }
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                failures.Add($"Missing package manifest: {manifestPath}");
            }
            else if (File.ReadAllText(manifestPath).Contains("com.unity.inputsystem"))
            {
                failures.Add("The Input System package is present; legacy Input Manager was required.");
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[PlayerSetup] {failure}");
                }

                throw new InvalidOperationException($"Player validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[PlayerSetup] Validation passed: Player prefab, camera, CharacterController, pause, interaction, prompt view, carry anchor, floor, and light are present; no Input System package.");
        }

        private static void CreatePlayerPrefab()
        {
            var root = new GameObject("Player");
            root.tag = "Player";

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            FirstPersonController movement = root.AddComponent<FirstPersonController>();
            PlayerInteractor interactor = root.AddComponent<PlayerInteractor>();
            PauseController pause = root.AddComponent<PauseController>();

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            SetObjectReference(movement, "_cameraTransform", cameraObject.transform);
            SetObjectReference(movement, "_pauseController", pause);
            SetObjectReference(interactor, "_camera", camera);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[PlayerSetup] Created player prefab: {AssetDatabase.GetAssetPath(prefab)}");
        }

        private static void EnsureCarrySetup()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                CarryController carry = contents.GetComponent<CarryController>();
                if (carry == null)
                {
                    carry = contents.AddComponent<CarryController>();
                }

                Transform anchor = contents.transform.Find("HoldAnchor");
                if (anchor == null)
                {
                    var anchorObject = new GameObject("HoldAnchor");
                    anchor = anchorObject.transform;
                    anchor.SetParent(contents.transform, false);
                    anchor.localPosition = new Vector3(0.28f, 1.2f, 0.55f);
                    anchor.localRotation = Quaternion.identity;
                    anchor.localScale = Vector3.one;
                }

                var serialized = new SerializedObject(carry);
                SerializedProperty property = serialized.FindProperty("_holdAnchor");
                if (property == null)
                {
                    throw new InvalidOperationException("Serialized field not found: _holdAnchor on CarryController");
                }

                property.objectReferenceValue = anchor;

                SerializedProperty dropKey = serialized.FindProperty("_dropKey");
                if (dropKey != null && dropKey.intValue == 0)
                {
                    dropKey.intValue = (int)KeyCode.Q;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsurePromptSetup()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                PlayerInteractor interactor = contents.GetComponent<PlayerInteractor>();
                if (interactor == null)
                {
                    throw new InvalidOperationException("Player prefab is missing PlayerInteractor.");
                }

                InteractionPromptView prompt = contents.GetComponent<InteractionPromptView>();
                if (prompt == null)
                {
                    prompt = contents.AddComponent<InteractionPromptView>();
                }

                var serialized = new SerializedObject(prompt);
                SerializedProperty property = serialized.FindProperty("_interactor");
                if (property == null)
                {
                    throw new InvalidOperationException("Serialized field not found: _interactor on InteractionPromptView");
                }

                property.objectReferenceValue = interactor;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                CarryController carry = contents.GetComponent<CarryController>();
                if (carry != null)
                {
                    SerializedProperty carryProperty = serialized.FindProperty("_carry");
                    if (carryProperty != null)
                    {
                        carryProperty.objectReferenceValue = carry;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureLaundromatScene()
        {
            Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            if (!HasRoot(scene, "GreyboxRoom"))
            {
                EnsureTemporaryFloor(scene);
            }

            EnsureDirectionalLight(scene);
            EnsurePlayerInstance(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureTemporaryFloor(Scene scene)
        {
            if (HasRoot(scene, "TemporaryFloor"))
            {
                return;
            }

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TemporaryFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            Debug.Log("[PlayerSetup] Added TemporaryFloor to Laundromat.");
        }

        private static void EnsureDirectionalLight(Scene scene)
        {
            if (HasRoot(scene, "Directional Light"))
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Debug.Log("[PlayerSetup] Added Directional Light to Laundromat.");
        }

        private static void EnsurePlayerInstance(Scene scene)
        {
            if (FindPlayer(scene) != null)
            {
                Debug.Log("[PlayerSetup] Keeping existing Player instance in Laundromat.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Cannot instantiate missing prefab: {PrefabPath}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = new Vector3(0f, 0.1f, 0f);
            Debug.Log("[PlayerSetup] Added Player to Laundromat.");
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

        private static bool HasRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidatePlayerObject(GameObject player, string label, List<string> failures)
        {
            if (player.GetComponent<CharacterController>() == null)
            {
                failures.Add($"{label}: missing CharacterController.");
            }

            if (player.GetComponent<FirstPersonController>() == null)
            {
                failures.Add($"{label}: missing FirstPersonController.");
            }

            if (player.GetComponent<PlayerInteractor>() == null)
            {
                failures.Add($"{label}: missing PlayerInteractor.");
            }

            if (player.GetComponent<PauseController>() == null)
            {
                failures.Add($"{label}: missing PauseController.");
            }

            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            InteractionPromptView prompt = player.GetComponent<InteractionPromptView>();
            if (prompt == null)
            {
                failures.Add($"{label}: missing InteractionPromptView.");
            }
            else if (prompt.Interactor == null)
            {
                failures.Add($"{label}: InteractionPromptView has no interactor reference.");
            }
            else if (prompt.Interactor != interactor)
            {
                failures.Add($"{label}: InteractionPromptView points at a different interactor.");
            }
            else if (prompt.Carry == null)
            {
                failures.Add($"{label}: InteractionPromptView has no carry reference.");
            }
            else if (prompt.Carry != player.GetComponent<CarryController>())
            {
                failures.Add($"{label}: InteractionPromptView points at a different carry controller.");
            }

            CarryController carry = player.GetComponent<CarryController>();
            if (carry == null)
            {
                failures.Add($"{label}: missing CarryController.");
            }
            else if (!carry.HasHoldAnchor)
            {
                failures.Add($"{label}: CarryController has no hold anchor.");
            }
            else if (carry.DropKey == KeyCode.None)
            {
                failures.Add($"{label}: CarryController has no drop key.");
            }

            if (player.transform.Find("HoldAnchor") == null)
            {
                failures.Add($"{label}: missing HoldAnchor child.");
            }

            Camera camera = player.GetComponentInChildren<Camera>(true);
            if (camera == null)
            {
                failures.Add($"{label}: missing camera.");
            }
            else if (camera.GetComponent<AudioListener>() == null)
            {
                failures.Add($"{label}: camera is missing an AudioListener.");
            }
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized field not found: {propertyName} on {target.name}");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsurePrefabFolder()
        {
            if (AssetDatabase.IsValidFolder(PrefabFolder))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/LaundryLegends/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/LaundryLegends", "Prefabs");
            }

            AssetDatabase.CreateFolder("Assets/LaundryLegends/Prefabs", "Gameplay");
        }
    }
}
