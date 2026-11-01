using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Imports the two MakeHuman variants and swaps them over the remaining NPCs,
    /// then applies the shop branding: logo shirt for the attendant and the
    /// logo fascia sign on the storefront.
    /// </summary>
    public static class NpcVariantTool
    {
        private const string ControllerPath = "Assets/LaundryLegends/Art/Animations/NPC_Locomotion.controller";
        private const string Materials = "Assets/LaundryLegends/Art/Materials";
        private const string Signs = "Assets/LaundryLegends/Art/Textures/Signs";
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";

        [MenuItem("Laundry Legends/Phase 8/Apply NPC Variants")]
        public static void Apply()
        {
            ConfigureImporter("Assets/LaundryLegends/Art/Models/NPC/Variants/AttendantHuman.fbx");
            ConfigureImporter("Assets/LaundryLegends/Art/Models/NPC/Variants/CustomerHuman.fbx");

            Swap(
                "Assets/LaundryLegends/Art/Models/NPC/Variants/AttendantHuman.fbx",
                "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Attendant.prefab",
                brandedShirt: true);

            Swap(
                "Assets/LaundryLegends/Art/Models/NPC/Variants/CustomerHuman.fbx",
                "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Customer.prefab",
                brandedShirt: false);

            ApplyFacadeLogo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VARIANT] Attendant and customer swapped; branding applied.");
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(path) as ModelImporter;
            }

            if (importer == null)
            {
                throw new System.InvalidOperationException($"No importer for {path}");
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.SaveAndReimport();
        }

        private static Avatar FindAvatar(string modelPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Avatar avatar && avatar.isHuman && avatar.isValid)
                {
                    Debug.Log($"[VARIANT] {modelPath}: avatar '{avatar.name}' human=True");
                    return avatar;
                }
            }

            return null;
        }

        private static void Swap(string modelPath, string prefabPath, bool brandedShirt)
        {
            Avatar avatar = FindAvatar(modelPath);
            if (avatar == null)
            {
                Debug.LogError($"[VARIANT] {modelPath} did not map as humanoid; skipping.");
                return;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Material shirt = brandedShirt ? EnsureBrandedShirt() : null;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform old = contents.transform.Find("Model");
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, contents.transform);
                instance.name = "Model";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                Animator animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                PrepareMaterials(instance, shirt, brandedShirt);
                if (brandedShirt)
                {
                    AddChestBadge(instance);
                }

                DisableHelperMesh(instance);

                NpcPose pose = contents.GetComponentInChildren<NpcPose>(true);
                if (pose != null)
                {
                    Object.DestroyImmediate(pose);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log($"[VARIANT] {prefabPath} now uses {System.IO.Path.GetFileName(modelPath)}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void PrepareMaterials(GameObject instance, Material brandedShirt, bool branded)
        {
            Material fallback = null;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.shader == null || material.shader.name == "Standard")
                    {
                        Color color = material.HasProperty("_Color") ? material.color : Color.white;
                        Texture texture = material.HasProperty("_MainTex") ? material.mainTexture : null;
                        material.shader = Shader.Find("Universal Render Pipeline/Lit");
                        material.SetColor("_BaseColor", color);
                        if (texture != null)
                        {
                            material.SetTexture("_BaseMap", texture);
                        }

                        material.SetFloat("_Smoothness", 0.25f);
                        EditorUtility.SetDirty(material);
                    }

                    materials[i] = material;
                    fallback ??= material;
                }

                renderer.sharedMaterials = materials;
            }

            if (fallback == null)
            {
                return;
            }

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool isShirt = branded && brandedShirt != null
                    && renderer.name.IndexOf("casualsuit", System.StringComparison.OrdinalIgnoreCase) >= 0;

                for (int i = 0; i < materials.Length; i++)
                {
                    bool broken = materials[i] == null
                        || materials[i].shader == null
                        || string.IsNullOrEmpty(materials[i].shader.name)
                        || materials[i].shader.name.Contains("InternalErrorShader");

                    if (isShirt && i == 0)
                    {
                        materials[i] = brandedShirt;
                    }
                    else if (broken)
                    {
                        materials[i] = fallback;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void DisableHelperMesh(GameObject instance)
        {
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.IndexOf("high-poly", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        private static Material EnsureBrandedShirt()
        {
            string path = $"{Materials}/MAT_ShirtBranded.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            // Plain shop navy: the logo is a separate chest badge, because the
            // casual-suit mesh covers shirt and trousers with one material.
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", new Color(0.13f, 0.17f, 0.24f));
            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_Metallic", 0f);
            material.SetTexture("_BaseMap", null);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddChestBadge(GameObject instance)
        {
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Signs}/logo_shirt.png");
            if (logo == null)
            {
                return;
            }

            string path = $"{Materials}/MAT_ShirtLogo.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", logo);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);

            var badge = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badge.name = "ChestBadge";
            Object.DestroyImmediate(badge.GetComponent<Collider>());
            badge.transform.SetParent(instance.transform, false);
            badge.transform.localPosition = new Vector3(0f, 1.32f, 0.15f);
            badge.transform.localRotation = Quaternion.identity;
            badge.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            badge.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void ApplyFacadeLogo()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MAT_Sign_sign_facade.mat");
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Signs}/sign_facade_logo.png");
            if (material == null || logo == null)
            {
                Debug.LogWarning("[VARIANT] Facade sign material or logo missing.");
                return;
            }

            material.SetTexture("_BaseMap", logo);
            EditorUtility.SetDirty(material);
            Debug.Log("[VARIANT] Storefront sign now uses the logo wordmark.");
        }
    }
}
