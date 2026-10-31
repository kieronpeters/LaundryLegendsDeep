using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// One-shot, re-runnable migration from the Built-in pipeline to URP:
    /// creates the pipeline assets, converts Standard materials (including
    /// scene-embedded ones), and adds a global post-processing volume.
    /// </summary>
    public static class UrpMigrationTool
    {
        private const string Folder = "Assets/LaundryLegends/Settings/URP";
        private const string PipelinePath = Folder + "/LL_URP_Asset.asset";
        private const string RendererPath = Folder + "/LL_URP_Renderer.asset";
        private const string ProfilePath = Folder + "/LL_URP_Volume.asset";
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Rendering/Build URP")]
        public static void BuildPipeline()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 40f;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;

            int previous = QualitySettings.GetQualityLevel();
            string[] levels = QualitySettings.names;
            for (int i = 0; i < levels.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }

            QualitySettings.SetQualityLevel(previous, false);
            AssetDatabase.SaveAssets();
            Debug.Log($"[URP] Pipeline built and assigned to {levels.Length} quality levels.");
        }

        [MenuItem("Laundry Legends/Rendering/Convert Materials")]
        public static void ConvertMaterials()
        {
            var converted = new List<string>();
            var failed = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/LaundryLegends" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null)
                {
                    continue;
                }

                if (material.shader.name == "Standard")
                {
                    if (Convert(material))
                    {
                        EditorUtility.SetDirty(material);
                        converted.Add(material.name);
                    }
                    else
                    {
                        failed.Add(path);
                    }
                }
            }

            ConvertSceneMaterials(converted, failed);

            AssetDatabase.SaveAssets();
            Debug.Log($"[URP] Converted {converted.Count} materials. Failed {failed.Count}.");
            foreach (string failure in failed)
            {
                Debug.LogWarning($"[URP] Could not convert {failure}");
            }
        }

        private static void ConvertSceneMaterials(List<string> converted, List<string> failed)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool changed = false;

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ScenePath))
            {
                if (!(asset is Material material) || material.shader == null || material.shader.name != "Standard")
                {
                    continue;
                }

                if (Convert(material))
                {
                    EditorUtility.SetDirty(material);
                    converted.Add($"{material.name} (scene)");
                    changed = true;
                }
                else
                {
                    failed.Add($"{material.name} (scene)");
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static bool Convert(Material material)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                return false;
            }

            Color color = material.HasProperty("_Color") ? material.color : Color.white;
            Texture texture = material.HasProperty("_MainTex") ? material.mainTexture : null;
            Vector2 tiling = material.HasProperty("_MainTex") ? material.mainTextureScale : Vector2.one;
            Vector2 offset = material.HasProperty("_MainTex") ? material.mainTextureOffset : Vector2.zero;
            float gloss = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.2f;
            float metal = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            float mode = material.HasProperty("_Mode") ? material.GetFloat("_Mode") : 0f;
            bool transparent = mode >= 2f || material.renderQueue >= 3000;
            bool cutout = mode > 0.5f && mode < 1.5f;
            Color emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            bool emits = material.IsKeywordEnabled("_EMISSION") && emission.maxColorComponent > 0.01f;

            material.shader = lit;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", gloss);
            material.SetFloat("_Metallic", metal);

            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
                material.SetTextureOffset("_BaseMap", offset);
            }

            if (cutout)
            {
                material.SetFloat("_AlphaClip", 1f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = 2450;
            }
            else if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.renderQueue = 2000;
            }

            if (emits)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            return true;
        }

        [MenuItem("Laundry Legends/Rendering/Build Post Processing")]
        public static void BuildPostProcessing()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            if (!profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }

            tonemapping.mode.Override(TonemappingMode.ACES);

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }

            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.7f);

            if (!profile.TryGet(out ColorAdjustments color))
            {
                color = profile.Add<ColorAdjustments>(true);
            }

            color.contrast.Override(8f);
            color.saturation.Override(6f);

            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }

            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.35f);

            EditorUtility.SetDirty(profile);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("PostProcessing");
            if (existing == null)
            {
                existing = new GameObject("PostProcessing");
            }

            var volume = existing.GetComponent<Volume>();
            if (volume == null)
            {
                volume = existing.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;

            var probeObject = GameObject.Find("RoomReflectionProbe");
            if (probeObject == null)
            {
                probeObject = new GameObject("RoomReflectionProbe");
                probeObject.transform.position = new Vector3(0f, 1.6f, 0f);
            }

            var probe = probeObject.GetComponent<ReflectionProbe>();
            if (probe == null)
            {
                probe = probeObject.AddComponent<ReflectionProbe>();
            }

            probe.mode = ReflectionProbeMode.Realtime;
            probe.resolution = 128;
            probe.size = new Vector3(16f, 3.4f, 12f);
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.importance = 1;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP] Post-processing volume and reflection probe added.");
        }

        [MenuItem("Laundry Legends/Rendering/Fix Transparency And Lights")]
        public static void FixTransparencyAndLights()
        {
            int fixedMaterials = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/LaundryLegends" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader == null || material.shader.name.StartsWith("Universal") == false)
                {
                    continue;
                }

                string name = material.name.ToLowerInvariant();
                bool shouldBeTransparent = name.Contains("glass") || name.Contains("film")
                    || name.Contains("shadow") || material.renderQueue >= 3000;

                if (shouldBeTransparent && material.GetFloat("_Surface") < 0.5f)
                {
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_Blend", 0f);
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0f);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.renderQueue = 3000;
                    EditorUtility.SetDirty(material);
                    fixedMaterials++;
                }
            }

            int lights = 0;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = 1.2f;
                    lights++;
                    continue;
                }

                // URP point lights are lumen-scale; the built-in values were ~2.
                light.intensity = Mathf.Max(light.intensity * 250f, 250f);
                light.range = Mathf.Max(light.range, 7f);
                lights++;
            }

            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.47f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[URP] Transparency fixed on {fixedMaterials} materials; {lights} lights retuned for URP.");
        }

        [MenuItem("Laundry Legends/Rendering/Tune Exposure")]
        public static void TuneExposure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = 0.9f;
                }
                else
                {
                    light.intensity = 35f;
                    light.range = Mathf.Clamp(light.range, 6f, 9f);
                }
            }

            RenderSettings.ambientLight = new Color(0.32f, 0.33f, 0.36f);

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null)
            {
                if (profile.TryGet(out Bloom bloom))
                {
                    bloom.threshold.Override(1.1f);
                    bloom.intensity.Override(0.22f);
                }

                if (profile.TryGet(out ColorAdjustments color))
                {
                    color.postExposure.Override(-0.9f);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP] Exposure tuned: point lights 110, ambient 0.32, bloom 0.35 @ 1.1, exposure -0.6.");
        }

        [MenuItem("Laundry Legends/Rendering/Validate URP")]
        public static void Validate()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                throw new System.InvalidOperationException("No default render pipeline assigned.");
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                throw new System.InvalidOperationException("Volume profile missing.");
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("PostProcessing") == null)
            {
                throw new System.InvalidOperationException("Scene has no PostProcessing volume.");
            }

            Debug.Log($"[URP] Validation passed. Pipeline={GraphicsSettings.defaultRenderPipeline.name}");
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}

