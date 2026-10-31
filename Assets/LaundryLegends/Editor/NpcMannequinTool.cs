using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Rebuilds the NPC prefabs on the animation library's own rigged mannequin.
    ///
    /// The 100 Avatars rigs produce absurd humanoid avatars under every import
    /// setting we tested (hips at 3.0 against a sub-millimetre mesh, or 46 m at
    /// scale 15.5), so retargeting places them kilometres from the room. The
    /// mannequin shipped with the animation pack is the rig the clips were
    /// authored for, so retargeting is exact.
    /// </summary>
    public static class NpcMannequinTool
    {
        private const string ModelPath = "Assets/LaundryLegends/Art/Animations/UAL1_Standard.fbx";
        private const string ControllerPath = "Assets/LaundryLegends/Art/Animations/NPC_Locomotion.controller";
        private const string PrefabFolder = "Assets/LaundryLegends/Prefabs/Gameplay/NPC";

        private static readonly (string Id, Color Tint)[] Specs =
        {
            ("NPC_Attendant", new Color(0.42f, 0.33f, 0.24f)),
            ("NPC_Customer", new Color(0.24f, 0.34f, 0.50f)),
            ("NPC_Regular", new Color(0.38f, 0.30f, 0.46f))
        };

        [MenuItem("Laundry Legends/Phase 4/Rebuild NPCs On Mannequin")]
        public static void Rebuild()
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ControllerPath);
            if (modelPrefab == null || controller == null)
            {
                throw new System.InvalidOperationException("Missing animation model or locomotion controller.");
            }

            Avatar avatar = FindAvatar();
            if (avatar == null)
            {
                throw new System.InvalidOperationException("Animation model has no humanoid avatar.");
            }

            foreach ((string id, Color tint) in Specs)
            {
                Material material = EnsureTintedMaterial(id, tint);
                BuildPrefab(id, modelPrefab, avatar, controller, material);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MANNEQUIN] NPC prefabs rebuilt on the animation rig.");
        }

        [MenuItem("Laundry Legends/Phase 4/Capture Mannequin NPC")]
        public static void CaptureOne()
        {
            var scene = EditorSceneManager.OpenScene("Assets/LaundryLegends/Scenes/Laundromat.unity", OpenSceneMode.Single);
            GameObject root = GameObject.Find("AmbientActors");
            Transform attendant = root != null ? root.transform.Find("Attendant") : null;
            if (attendant == null)
            {
                throw new System.InvalidOperationException("No Attendant in scene.");
            }

            Renderer[] renderers = attendant.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Debug.Log($"[MANNEQUIN] renderer '{renderer.name}' type={renderer.GetType().Name} bounds={renderer.bounds}");
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        private static Avatar FindAvatar()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is Avatar avatar && avatar.isHuman)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static Material EnsureTintedMaterial(string id, Color tint)
        {
            string path = $"Assets/LaundryLegends/Art/Materials/MAT_{id}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = $"MAT_{id}" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Standard");
            material.color = tint;
            material.mainTexture = null;
            material.SetFloat("_Glossiness", 0.22f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildPrefab(
            string id,
            GameObject modelPrefab,
            Avatar avatar,
            UnityEditor.Animations.AnimatorController controller,
            Material material)
        {
            string prefabPath = $"{PrefabFolder}/{id}.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform old = contents.transform.Find("Model");
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, contents.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }

                Animator animator = model.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = model.AddComponent<Animator>();
                }

                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                NpcPose pose = contents.GetComponentInChildren<NpcPose>(true);
                if (pose != null)
                {
                    Object.DestroyImmediate(pose);
                }

                if (contents.GetComponent<NpcLocomotion>() == null)
                {
                    contents.AddComponent<NpcLocomotion>();
                }

                if (contents.GetComponent<CarryPose>() == null)
                {
                    contents.AddComponent<CarryPose>();
                }

                Transform anchor = contents.transform.Find("CarryAnchor");
                if (anchor == null)
                {
                    var anchorObject = new GameObject("CarryAnchor");
                    anchor = anchorObject.transform;
                    anchor.SetParent(contents.transform, false);
                    anchor.localPosition = new Vector3(0f, 1.15f, 0.34f);
                }

                CarrySocket socket = contents.GetComponent<CarrySocket>();
                if (socket == null)
                {
                    socket = contents.AddComponent<CarrySocket>();
                }

                socket.Configure(anchor);

                CapsuleCollider capsule = contents.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    capsule.center = new Vector3(0f, 0.9f, 0f);
                    capsule.height = 1.8f;
                    capsule.radius = 0.3f;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log($"[MANNEQUIN] {id} rebuilt.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
