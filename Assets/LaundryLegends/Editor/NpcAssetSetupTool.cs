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
    /// Imports the three CC0 100 Avatars humanoids, builds simple NPC prefabs
    /// with capsule colliders, and records whether animation clips are present.
    /// No behavior scripts are added in this asset phase.
    /// </summary>
    public static class NpcAssetSetupTool
    {
        private const string ModelFolder = "Assets/LaundryLegends/Art/Models/NPC";
        private const string TextureFolder = "Assets/LaundryLegends/Art/Textures/NPC";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string PrefabFolder = "Assets/LaundryLegends/Prefabs/Gameplay/NPC";

        private static readonly NpcSpec[] Specs =
        {
            new NpcSpec("NPC_Customer", "Customer", new Color(0.35f, 0.55f, 0.85f)),
            new NpcSpec("NPC_Attendant", "Attendant", new Color(0.85f, 0.65f, 0.25f)),
            new NpcSpec("NPC_Regular", "Regular", new Color(0.55f, 0.45f, 0.75f))
        };

        [MenuItem("Laundry Legends/Phase 3/Apply NPC Archetypes")]
        public static void ApplyNpcArchetypes()
        {
            EnsureFolder("Assets/LaundryLegends/Art/Models", "NPC");
            EnsureFolder("Assets/LaundryLegends/Art/Textures", "NPC");
            EnsureFolder("Assets/LaundryLegends/Prefabs/Gameplay", "NPC");

            foreach (NpcSpec spec in Specs)
            {
                string modelPath = $"{ModelFolder}/{spec.Id}.fbx";
                string texturePath = $"{TextureFolder}/{spec.Id}.png";
                string materialPath = $"{MaterialFolder}/MAT_{spec.Id}.mat";
                string prefabPath = $"{PrefabFolder}/{spec.Id}.prefab";

                ConfigureImporter(modelPath);
                Material material = EnsureMaterial(materialPath, spec.Id, texturePath, spec.Tint);
                BuildPrefab(spec, modelPath, material, prefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NPC] Three CC0 NPC archetypes applied.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate NPC Archetypes")]
        public static void ValidateNpcArchetypes()
        {
            var failures = new List<string>();

            foreach (NpcSpec spec in Specs)
            {
                string modelPath = $"{ModelFolder}/{spec.Id}.fbx";
                string prefabPath = $"{PrefabFolder}/{spec.Id}.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null)
                {
                    failures.Add($"Missing NPC model: {modelPath}");
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    failures.Add($"Missing NPC prefab: {prefabPath}");
                    continue;
                }

                if (prefab.GetComponent<CapsuleCollider>() == null)
                {
                    failures.Add($"{prefabPath} has no capsule collider.");
                }

                if (prefab.GetComponent<Rigidbody>() != null)
                {
                    failures.Add($"{prefabPath} should not have a Rigidbody.");
                }

                Transform model = prefab.transform.Find("Model");
                if (model == null)
                {
                    failures.Add($"{prefabPath} has no Model child.");
                }
                else
                {
                    Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0)
                    {
                        failures.Add($"{prefabPath} has no model renderers.");
                    }

                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.sharedMaterial == null)
                        {
                            failures.Add($"{prefabPath} has an unassigned material.");
                        }
                    }

                    if (renderers.Length > 0)
                    {
                        Vector3 size = renderers[0].bounds.size;
                        Debug.Log($"[NPC] {spec.Id}: render bounds {size}.");
                        if (size.y < 1.2f || size.y > 2.2f)
                        {
                            failures.Add($"{prefabPath} imported at an unexpected height: {size.y:0.00} m.");
                        }
                    }

                    MeshFilter filter = model.GetComponentInChildren<MeshFilter>(true);
                    if (filter != null && filter.sharedMesh != null)
                    {
                        int triangles = filter.sharedMesh.triangles.Length / 3;
                        if (triangles > 25000)
                        {
                            failures.Add($"{prefabPath} exceeds the 25,000-triangle NPC budget: {triangles}.");
                        }
                    }
                }

                Animator animator = prefab.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    Debug.Log($"[NPC] {spec.Id}: no Animator; static fallback documented.");
                }
                else if (animator.avatar != null)
                {
                    Debug.Log($"[NPC] {spec.Id}: avatar humanoid={animator.avatar.isHuman}.");
                }

                SkinnedMeshRenderer skinned = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skinned != null)
                {
                    foreach (Transform bone in skinned.bones)
                    {
                        if (bone.name.IndexOf("Arm", StringComparison.OrdinalIgnoreCase) >= 0
                            || bone.name.IndexOf("Shoulder", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Debug.Log($"[NPC] {spec.Id}: bone {bone.name} localRot {bone.localRotation.eulerAngles}.");
                        }
                    }
                }

                int clipCount = CountAnimationClips(modelPath);
                Debug.Log($"[NPC] {spec.Id}: imported animation clips={clipCount}.");
                foreach (AnimationClip clip in LoadAnimationClips(modelPath))
                {
                    Debug.Log($"[NPC] {spec.Id}: clip '{clip.name}' length {clip.length:0.00}s.");
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[NPC] {failure}");
                }

                throw new InvalidOperationException($"NPC validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[NPC] Validation passed: three prefabs, capsule colliders, materials, and models are intact.");
        }

        [MenuItem("Laundry Legends/Phase 3/Pose NPC Arms")]
        public static void PoseNpcArms()
        {
            foreach (NpcSpec spec in Specs)
            {
                string prefabPath = $"{PrefabFolder}/{spec.Id}.prefab";
                GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    SkinnedMeshRenderer skinned = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    if (skinned == null)
                    {
                        continue;
                    }

                    foreach (Transform bone in skinned.bones)
                    {
                        if (bone.name.EndsWith("LeftArm", StringComparison.Ordinal))
                        {
                            bone.localRotation = Quaternion.Euler(0f, 0f, 55f) * bone.localRotation;
                        }
                        else if (bone.name.EndsWith("RightArm", StringComparison.Ordinal))
                        {
                            bone.localRotation = Quaternion.Euler(0f, 0f, -55f) * bone.localRotation;
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[NPC] Applied a relaxed arm pose to the three NPC prefabs.");
        }

        [MenuItem("Laundry Legends/Phase 3/Capture NPC Archetypes")]
        public static void CaptureNpcArchetypes()
        {
            EditorSceneManager.OpenScene("Assets/LaundryLegends/Scenes/Laundromat.unity", OpenSceneMode.Single);
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var cameraObject = new GameObject("NpcPreviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.fieldOfView = 45f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.18f, 0.19f, 0.21f);
                cameraObject.transform.position = new Vector3(0f, 1.35f, -4.2f);
                cameraObject.transform.LookAt(new Vector3(0f, 1.0f, 0f));

                var lightObject = new GameObject("NpcPreviewLight");
                SceneManager.MoveGameObjectToScene(lightObject, preview);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

                for (int i = 0; i < Specs.Length; i++)
                {
                    string prefabPath = $"{PrefabFolder}/{Specs[i].Id}.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    SceneManager.MoveGameObjectToScene(instance, preview);
                    instance.transform.position = new Vector3((i - 1) * 1.4f, 0f, 0f);
                    instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }

                var renderTexture = new RenderTexture(1280, 720, 24);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;

                var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();

                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, "npc-archetypes.png"), texture.EncodeToPNG());

                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
            finally
            {
                EditorSceneManager.CloseScene(preview, true);
            }

            Debug.Log("[NPC] Captured TestResults/npc-archetypes.png.");
        }

        private static void ConfigureImporter(string modelPath)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Missing model importer: {modelPath}");
            }

            // 100 Avatars FBX exports are about 0.12 m tall at scale 1; 14x
            // brings the imported humanoid to roughly 1.7 m for this project.
            importer.globalScale = 14f;
            importer.useFileScale = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        private static Material EnsureMaterial(string materialPath, string id, string texturePath, Color tint)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                return material;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material = new Material(Shader.Find("Standard"))
            {
                name = $"MAT_{id}",
                color = Color.white
            };
            if (texture != null)
            {
                material.mainTexture = texture;
            }
            else
            {
                material.color = tint;
            }

            material.SetFloat("_Glossiness", 0.1f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static void BuildPrefab(NpcSpec spec, string modelPath, Material material, string prefabPath)
        {
            var root = new GameObject(spec.Id);
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.3f;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException($"Missing NPC model: {modelPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = "Model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static int CountAnimationClips(string modelPath)
        {
            return LoadAnimationClips(modelPath).Count;
        }

        private static System.Collections.Generic.List<AnimationClip> LoadAnimationClips(string modelPath)
        {
            var clips = new System.Collections.Generic.List<AnimationClip>();
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is AnimationClip clip)
                {
                    clips.Add(clip);
                }
            }

            return clips;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string grandParent = Path.GetDirectoryName(parent)?.Replace('\\', '/');
                string parentName = Path.GetFileName(parent);
                if (!string.IsNullOrEmpty(grandParent))
                {
                    EnsureFolder(grandParent, parentName);
                }
            }

            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private sealed class NpcSpec
        {
            public NpcSpec(string id, string role, Color tint)
            {
                Id = id;
                Role = role;
                Tint = tint;
            }

            public string Id { get; }

            public string Role { get; }

            public Color Tint { get; }
        }
    }
}
