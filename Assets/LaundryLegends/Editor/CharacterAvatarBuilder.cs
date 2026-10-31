using System.Collections.Generic;
using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds a humanoid Avatar explicitly for the Quaternius character and
    /// swaps the NPC prefabs onto it.
    ///
    /// Unity's auto-mapper refuses this rig (it imports as human=False) even
    /// after renaming bones, so the human bone mapping is declared by hand with
    /// AvatarBuilder. Everything else - skeleton, clips, controller - is shared.
    /// </summary>
    public static class CharacterAvatarBuilder
    {
        private const string ModelPath = "Assets/LaundryLegends/Art/Models/NPC/QuaterniusMan.fbx";
        private const string AvatarPath = "Assets/LaundryLegends/Art/Animations/QuaterniusManAvatar.asset";
        private const string ControllerPath = "Assets/LaundryLegends/Art/Animations/NPC_Locomotion.controller";
        private const string PrefabFolder = "Assets/LaundryLegends/Prefabs/Gameplay/NPC";

        private static readonly (string Bone, string Human)[] HumanMap =
        {
            ("Hips", "Hips"),
            ("Spine", "Spine"),
            ("Chest", "Chest"),
            ("Neck", "Neck"),
            ("Head", "Head"),
            ("LeftShoulder", "LeftShoulder"),
            ("LeftUpperArm", "LeftUpperArm"),
            ("LeftLowerArm", "LeftLowerArm"),
            ("LeftHand", "LeftHand"),
            ("RightShoulder", "RightShoulder"),
            ("RightUpperArm", "RightUpperArm"),
            ("RightLowerArm", "RightLowerArm"),
            ("RightHand", "RightHand"),
            ("LeftUpperLeg", "LeftUpperLeg"),
            ("LeftLowerLeg", "LeftLowerLeg"),
            ("LeftFoot", "LeftFoot"),
            ("RightUpperLeg", "RightUpperLeg"),
            ("RightLowerLeg", "RightLowerLeg"),
            ("RightFoot", "RightFoot")
        };

        private static readonly (string Id, Color Shirt)[] Specs =
        {
            ("NPC_Attendant", new Color(0.58f, 0.42f, 0.26f)),
            ("NPC_Customer", new Color(0.30f, 0.45f, 0.66f)),
            ("NPC_Regular", new Color(0.45f, 0.36f, 0.58f))
        };

        [MenuItem("Laundry Legends/Phase 4/Build Character Avatar")]
        public static void BuildEverything()
        {
            Avatar avatar = BuildAvatar();
            foreach ((string id, Color shirt) in Specs)
            {
                SwapPrefab(id, avatar, shirt);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CHAR] Character avatar built and NPCs swapped.");
        }

        private static Avatar BuildAvatar()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new System.InvalidOperationException($"Missing {ModelPath}");
            }

            var instance = Object.Instantiate(model);
            try
            {
                Avatar generic = null;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
                {
                    if (asset is Avatar candidate)
                    {
                        generic = candidate;
                        break;
                    }
                }

                if (generic == null)
                {
                    throw new System.InvalidOperationException("Model has no avatar to read the skeleton from.");
                }

                HumanDescription description = generic.humanDescription;

                // A generic (non-humanoid) avatar exposes an empty skeleton, which
                // is why BuildHumanAvatar failed. Rebuild it from the live rig.
                if (description.skeleton == null || description.skeleton.Length == 0)
                {
                    var skeleton = new List<SkeletonBone>();
                    foreach (Transform transform in instance.GetComponentsInChildren<Transform>(true))
                    {
                        skeleton.Add(new SkeletonBone
                        {
                            name = transform.name,
                            position = transform.localPosition,
                            rotation = transform.localRotation,
                            scale = transform.localScale
                        });
                    }

                    description.skeleton = skeleton.ToArray();
                    Debug.Log($"[CHAR] Rebuilt skeleton from {skeleton.Count} transforms.");
                }

                var human = new List<HumanBone>();
                foreach ((string bone, string humanName) in HumanMap)
                {
                    human.Add(new HumanBone
                    {
                        boneName = bone,
                        humanName = humanName,
                        limit = new HumanLimit { useDefaultValues = true }
                    });
                }

                description.human = human.ToArray();

                Avatar avatar = AvatarBuilder.BuildHumanAvatar(instance, description);
                if (avatar == null || !avatar.isValid || !avatar.isHuman)
                {
                    throw new System.InvalidOperationException(
                        $"Built avatar is not humanoid (valid={avatar?.isValid}, human={avatar?.isHuman}).");
                }

                avatar.name = "QuaterniusManHumanoid";
                AssetDatabase.DeleteAsset(AvatarPath);
                AssetDatabase.CreateAsset(avatar, AvatarPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"[CHAR] Avatar built: human={avatar.isHuman} valid={avatar.isValid} -> {AvatarPath}");
                return avatar;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void SwapPrefab(string id, Avatar avatar, Color shirtTint)
        {
            string prefabPath = $"{PrefabFolder}/{id}.prefab";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

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

                // Tint the shirt so the three NPCs read as different people.
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null)
                        {
                            continue;
                        }

                        if (materials[i].name.IndexOf("Shirt", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var tinted = new Material(materials[i]);
                            tinted.color = shirtTint;
                            materials[i] = tinted;
                        }
                    }

                    renderer.sharedMaterials = materials;
                }

                var socket = contents.GetComponent<CarrySocket>();
                Transform anchor = contents.transform.Find("CarryAnchor");
                if (anchor == null)
                {
                    var anchorObject = new GameObject("CarryAnchor");
                    anchor = anchorObject.transform;
                    anchor.SetParent(contents.transform, false);
                    anchor.localPosition = new Vector3(0f, 1.15f, 0.34f);
                }

                if (socket != null)
                {
                    socket.Configure(anchor);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log($"[CHAR] {id} swapped to Quaternius character.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
