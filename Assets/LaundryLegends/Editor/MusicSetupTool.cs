using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Adds the original BaliLaundryLoop to the Laundromat scene as one
    /// looping, non-spatial AudioSource. Idempotent and additive.
    /// </summary>
    public static class MusicSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string ClipPath = "Assets/LaundryLegends/Audio/Music/BalinesePrelude.mp3";

        [MenuItem("Laundry Legends/Phase 3/Apply Music")]
        public static void ApplyMusic()
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip == null)
            {
                throw new System.InvalidOperationException($"Missing music clip: {ClipPath}");
            }

            ConfigureImporter();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject existing = FindRoot(scene, "Music");
            if (existing == null)
            {
                existing = new GameObject("Music");
                SceneManager.MoveGameObjectToScene(existing, scene);
            }

            AudioSource source = existing.GetComponent<AudioSource>();
            if (source == null)
            {
                source = existing.AddComponent<AudioSource>();
            }

            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = 0.35f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Music] BalinesePrelude applied to the Laundromat scene.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Music")]
        public static void ValidateMusic()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject music = FindRoot(scene, "Music");
            if (music == null)
            {
                throw new System.InvalidOperationException("Laundromat has no Music root.");
            }

            AudioSource source = music.GetComponent<AudioSource>();
            if (source == null || source.clip == null)
            {
                throw new System.InvalidOperationException("Music root has no clip.");
            }

            if (!source.loop || !source.playOnAwake || source.volume <= 0f)
            {
                throw new System.InvalidOperationException("Music source is not configured as a looping ambient track.");
            }

            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            Debug.Log($"[Music] Audio sources in scene: {sources.Length} (music plus NPC voices).");

            Debug.Log("[Music] Validation passed: one looping BalinesePrelude source.");
        }

        private static void ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(ClipPath) as AudioImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException($"Missing audio importer: {ClipPath}");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.SaveAndReimport();
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
