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
