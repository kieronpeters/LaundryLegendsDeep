using System;
using System.Collections.Generic;
using System.IO;
using LaundryLegends.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Creates and validates the foundation scenes and their build-settings order.
    /// Keeping this in the editor assembly makes the setup reproducible from Git.
    /// </summary>
    public static class SceneSetupTool
    {
        private const string ScenesFolder = "Assets/LaundryLegends/Scenes";
        private static readonly string[] SceneNames = { "Bootstrap", "MainMenu", "Laundromat" };

        [MenuItem("Laundry Legends/Setup Foundation Scenes")]
        public static void CreateFoundationScenes()
        {
            EnsureScenesFolderExists();

            foreach (string sceneName in SceneNames)
            {
                string scenePath = GetScenePath(sceneName);
                if (File.Exists(scenePath))
                {
                    Debug.Log($"[SceneSetup] Keeping existing scene: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    throw new IOException($"Failed to save scene: {scenePath}");
                }

                Debug.Log($"[SceneSetup] Created scene: {scenePath}");
            }

            RegisterScenesInBuildSettings();
            ConfigureFoundationScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] Foundation scenes are ready.");
        }

        [MenuItem("Laundry Legends/Validate Foundation Scenes")]
        public static void ValidateFoundationScenes()
        {
            var failures = new List<string>();

            foreach (string sceneName in SceneNames)
            {
                string scenePath = GetScenePath(sceneName);
                if (!File.Exists(scenePath))
                {
                    failures.Add($"Missing scene file: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                    if (missingCount > 0)
                    {
                        failures.Add($"{scenePath}: {missingCount} missing script(s) on '{root.name}'.");
                    }
                }

                if (sceneName == "Bootstrap")
                {
                    RequireComponent<GameSession>(scene, scenePath, failures);
                    RequireComponent<BootstrapController>(scene, scenePath, failures);
                }
                else if (sceneName == "MainMenu")
                {
                    RequireComponent<MainMenuController>(scene, scenePath, failures);
                }

                Debug.Log($"[SceneSetup] Opened scene without load errors: {scenePath}");
            }

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length != SceneNames.Length)
            {
                failures.Add($"Build settings contain {buildScenes.Length} scene(s); expected {SceneNames.Length}.");
            }
            else
            {
                for (int i = 0; i < SceneNames.Length; i++)
                {
                    string expectedPath = GetScenePath(SceneNames[i]);
                    if (buildScenes[i].path != expectedPath)
                    {
                        failures.Add($"Build settings index {i} is '{buildScenes[i].path}'; expected '{expectedPath}'.");
                    }

                    if (!buildScenes[i].enabled)
                    {
                        failures.Add($"Build settings scene is disabled: {expectedPath}");
                    }
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[SceneSetup] {failure}");
                }

                throw new InvalidOperationException($"Scene validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[SceneSetup] Validation passed: three scenes present, Bootstrap at index 0, no missing scripts.");
        }

        private static void ConfigureFoundationScenes()
        {
            ConfigureBootstrapScene();
            ConfigureMainMenuScene();
        }

        private static void ConfigureBootstrapScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GetScenePath("Bootstrap"), OpenSceneMode.Single);
            GameObject bootstrapObject = FindOrCreateRoot(scene, "Bootstrap");

            if (bootstrapObject.GetComponent<GameSession>() == null)
            {
                bootstrapObject.AddComponent<GameSession>();
            }

            if (bootstrapObject.GetComponent<BootstrapController>() == null)
            {
                bootstrapObject.AddComponent<BootstrapController>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] Configured Bootstrap scene.");
        }

        private static void ConfigureMainMenuScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GetScenePath("MainMenu"), OpenSceneMode.Single);
            GameObject menuObject = FindOrCreateRoot(scene, "MainMenu");

            if (menuObject.GetComponent<MainMenuController>() == null)
            {
                menuObject.AddComponent<MainMenuController>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] Configured MainMenu scene.");
        }

        private static GameObject FindOrCreateRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return new GameObject(objectName);
        }

        private static void RequireComponent<T>(Scene scene, string scenePath, List<string> failures) where T : Component
        {
            if (!HasComponentInScene<T>(scene))
            {
                failures.Add($"{scenePath}: missing required component {typeof(T).Name}.");
            }
        }

        private static bool HasComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<T>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureScenesFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets/LaundryLegends", "Scenes");
            }
        }

        private static void RegisterScenesInBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[SceneNames.Length];
            for (int i = 0; i < SceneNames.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(GetScenePath(SceneNames[i]), true);
            }

            EditorBuildSettings.scenes = scenes;
        }

        private static string GetScenePath(string sceneName)
        {
            return $"{ScenesFolder}/{sceneName}.unity";
        }
    }
}
