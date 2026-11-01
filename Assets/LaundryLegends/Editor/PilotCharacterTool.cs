using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Imports the MakeHuman pilot as a humanoid and swaps it over one NPC
    /// (the phone regular) so the realism can be judged in game.
    /// </summary>
    public static class PilotCharacterTool
    {
        private const string ModelPath = "Assets/LaundryLegends/Art/Models/NPC/Pilot/PilotHuman.fbx";
        private const string PrefabPath = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Regular.prefab";
        private const string ControllerPath = "Assets/LaundryLegends/Art/Animations/NPC_Locomotion.controller";

        [MenuItem("Laundry Legends/Phase 8/Import Pilot Character")]
        public static void Import()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            }

            if (importer == null)
            {
                throw new System.InvalidOperationException($"No importer for {ModelPath}");
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

            Avatar avatar = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is Avatar candidate)
                {
                    avatar = candidate;
                    break;
                }
            }

            if (avatar == null)
            {
                Debug.LogError("[PILOT] No avatar created.");
                return;
            }

            Debug.Log($"[PILOT] avatar={avatar.name} human={avatar.isHuman} valid={avatar.isValid}");

            int shown = 0;
            foreach (SkeletonBone bone in avatar.humanDescription.skeleton)
            {
                if (shown++ < 12)
                {
                    Debug.Log($"[PILOT] bone '{bone.name}'");
                }
            }

            if (!avatar.isHuman)
            {
                Debug.LogError("[PILOT] Unity did not map this rig as humanoid; not swapping.");
                return;
            }

            Swap(avatar);
        }

        private static void Swap(Avatar avatar)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
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

                // Imported materials come in as Standard, which renders magenta in
                // URP; convert them and use the first valid one as a fallback.
                Material fallback = null;
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null)
                        {
                            continue;
                        }

                        Material material = materials[i];
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

                // MakeHuman exports an unused high-poly base mesh; it renders as a
                // stray slab and is not part of the visible character.
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

                if (fallback != null)
                {
                    foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            Material current = materials[i];
                            bool broken = current == null
                                || current.shader == null
                                || string.IsNullOrEmpty(current.shader.name)
                                || current.shader.name.Contains("InternalErrorShader");

                            if (broken)
                            {
                                materials[i] = fallback;
                            }
                        }

                        renderer.sharedMaterials = materials;
                    }
                }

                NpcPose pose = contents.GetComponentInChildren<NpcPose>(true);
                if (pose != null)
                {
                    Object.DestroyImmediate(pose);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                Debug.Log("[PILOT] NPC_Regular now uses the MakeHuman pilot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
