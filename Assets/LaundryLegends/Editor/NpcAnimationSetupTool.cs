using System;
using System.Collections.Generic;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the NPC locomotion animation layer: marks the Idle/Walk takes as
    /// looping, creates one shared humanoid controller, and wires each NPC
    /// prefab with an Animator on its Model child plus the carry components.
    /// Also fixes the pickup customer's stand point in the Laundromat scene.
    /// </summary>
    public static class NpcAnimationSetupTool
    {
        public const string AnimationFolder = "Assets/LaundryLegends/Art/Animations";
        public const string LibraryPath = AnimationFolder + "/UAL1_Standard.fbx";
        public const string ControllerPath = AnimationFolder + "/NPC_Locomotion.controller";
        public const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        public const string IdleClipName = "Idle_Loop";
        public const string WalkClipName = "Walk_Loop";
        public const string LoadClipName = "Interact";
        public const string FoldClipName = "PickUp_Table";
        public const string PrefabFolder = "Assets/LaundryLegends/Prefabs/Gameplay/NPC";

        private const string SpeedParameter = "Speed";
        private const string CarryingParameter = "Carrying";
        private const string LoadParameter = "Load";
        private const string FoldParameter = "Fold";
        private const string HandoffParameter = "Handoff";
        private const float IdleThreshold = 0.1f;

        private static readonly string[] PrefabIds =
        {
            "NPC_Attendant",
            "NPC_Customer",
            "NPC_Regular"
        };

        private static readonly Vector3 CarryAnchorLocalPosition = new Vector3(0f, 1.15f, 0.34f);
        private static readonly Vector3 TableStandPoint = new Vector3(2.55f, 0f, 1f);

        [MenuItem("Laundry Legends/Phase 4/Build NPC Animation")]
        public static void BuildNpcAnimation()
        {
            EnsureLoopingLocomotionTakes();
            AnimationClip idle = FindClip(IdleClipName);
            AnimationClip walk = FindClip(WalkClipName);
            if (idle == null || walk == null)
            {
                throw new InvalidOperationException(
                    $"Missing locomotion takes in {LibraryPath} (idle={idle != null}, walk={walk != null}).");
            }

            AnimatorController controller = BuildController(idle, walk);
            foreach (string id in PrefabIds)
            {
                ConfigurePrefab(id, controller);
            }

            FixSceneStandPoint();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NPC] NPC locomotion built: {ControllerPath} (Idle='{idle.name}', Walk='{walk.name}').");
        }

        [MenuItem("Laundry Legends/Phase 4/Validate NPC Animation")]
        public static void ValidateNpcAnimation()
        {
            var failures = new List<string>();

            if (AssetDatabase.LoadAllAssetsAtPath(LibraryPath).Length == 0)
            {
                failures.Add($"Missing animation library: {LibraryPath}");
            }

            AnimationClip idle = FindClip(IdleClipName);
            AnimationClip walk = FindClip(WalkClipName);
            if (idle == null)
            {
                failures.Add($"Missing clip '{IdleClipName}'.");
            }
            else if (!AnimationUtility.GetAnimationClipSettings(idle).loopTime)
            {
                failures.Add($"Clip '{idle.name}' does not loop.");
            }

            if (walk == null)
            {
                failures.Add($"Missing clip '{WalkClipName}'.");
            }
            else if (!AnimationUtility.GetAnimationClipSettings(walk).loopTime)
            {
                failures.Add($"Clip '{walk.name}' does not loop.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                failures.Add($"Missing controller: {ControllerPath}");
            }
            else
            {
                ValidateController(controller, idle, walk, failures);
            }

            foreach (string id in PrefabIds)
            {
                ValidatePrefab(id, controller, failures);
            }

            ValidateScene(failures);

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[NPC] {failure}");
                }

                throw new InvalidOperationException($"NPC animation validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[NPC] NPC animation validation passed: looping clips, controller, animators, carry components, and scene stand point.");
        }

        private static void EnsureLoopingLocomotionTakes()
        {
            var importer = AssetImporter.GetAtPath(LibraryPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Missing model importer: {LibraryPath}");
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;

            // Imported takes default to non-looping (clipAnimations is empty), so
            // the idle pose would freeze on the last frame after one pass.
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (!IsLocomotionTake(clip.name))
                {
                    continue;
                }

                clip.loopTime = true;
                clip.wrapMode = WrapMode.Loop;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            Debug.Log($"[NPC] Loop time enabled on the '{IdleClipName}'/'{WalkClipName}' takes.");
        }

        private static bool IsLocomotionTake(string takeName)
        {
            string segment = LastSegment(takeName);
            return segment == IdleClipName || segment == WalkClipName;
        }

        /// <summary>
        /// Unity names imported clips "file|Armature|Take"; matching the final
        /// segment keeps e.g. "Sitting_Idle_Loop" from masquerading as Idle_Loop.
        /// </summary>
        private static string LastSegment(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            int bar = name.LastIndexOf('|');
            return bar >= 0 ? name.Substring(bar + 1) : name;
        }

        private static AnimationClip FindClip(string takeName)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(LibraryPath))
            {
                if (asset is AnimationClip clip
                    && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)
                    && LastSegment(clip.name) == takeName)
                {
                    return clip;
                }
            }

            return null;
        }

        private static AnimatorController BuildController(AnimationClip idle, AnimationClip walk)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(CarryingParameter, AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = idle;
            idleState.speed = 1f;

            AnimatorState walkState = machine.AddState("Walk");
            walkState.motion = walk;
            walkState.speed = 1f;

            machine.defaultState = idleState;

            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.exitTime = 0f;
            toWalk.duration = 0.15f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, IdleThreshold, SpeedParameter);

            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.exitTime = 0f;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.Less, IdleThreshold, SpeedParameter);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return controller;
        }

        private static void ConfigurePrefab(string id, AnimatorController controller)
        {
            string prefabPath = $"{PrefabFolder}/{id}.prefab";
            string modelPath = $"Assets/LaundryLegends/Art/Models/NPC/{id}.fbx";

            Avatar avatar = LoadAvatar(modelPath);
            if (avatar == null)
            {
                throw new InvalidOperationException($"No humanoid avatar on {modelPath}.");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform model = root.transform.Find("Model");
                if (model == null)
                {
                    throw new InvalidOperationException($"{prefabPath} has no Model child.");
                }

                foreach (NpcPose pose in root.GetComponentsInChildren<NpcPose>(true))
                {
                    UnityEngine.Object.DestroyImmediate(pose);
                }

                Animator animator = model.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = model.gameObject.AddComponent<Animator>();
                }

                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                EnsureComponent<NpcLocomotion>(root);
                EnsureComponent<CarryPose>(root);
                CarrySocket socket = EnsureComponent<CarrySocket>(root);

                Transform anchor = root.transform.Find("CarryAnchor");
                if (anchor == null)
                {
                    var anchorObject = new GameObject("CarryAnchor");
                    anchor = anchorObject.transform;
                    anchor.SetParent(root.transform, false);
                }

                anchor.localPosition = CarryAnchorLocalPosition;
                anchor.localRotation = Quaternion.identity;
                anchor.localScale = Vector3.one;
                socket.Configure(anchor);
                EditorUtility.SetDirty(socket);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"[NPC] {id}: Animator on Model with its own avatar, NPC_Locomotion controller, carry components, CarryAnchor.");
        }

        private static T EnsureComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static Avatar LoadAvatar(string modelPath)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Avatar avatar && avatar.isHuman)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static void FixSceneStandPoint()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int fixedCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (PickupCustomer customer in root.GetComponentsInChildren<PickupCustomer>(true))
                {
                    var serialized = new SerializedObject(customer);
                    SerializedProperty stand = serialized.FindProperty("_tableStand");
                    if (stand == null)
                    {
                        throw new InvalidOperationException($"{customer.name} has no _tableStand field.");
                    }

                    stand.vector3Value = TableStandPoint;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(customer);
                    fixedCount++;
                }
            }

            if (fixedCount == 0)
            {
                Debug.LogWarning("[NPC] No PickupCustomer found in the Laundromat scene.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[NPC] PickupCustomer stand point set to {TableStandPoint} on {fixedCount} actor(s).");
        }

        private static void ValidateController(
            AnimatorController controller,
            AnimationClip idle,
            AnimationClip walk,
            List<string> failures)
        {
            bool hasSpeed = false;
            bool hasCarrying = false;
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == SpeedParameter && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasSpeed = true;
                }
                else if (parameter.name == CarryingParameter && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasCarrying = true;
                }
            }

            if (!hasSpeed)
            {
                failures.Add($"Controller is missing the float '{SpeedParameter}' parameter.");
            }

            if (!hasCarrying)
            {
                failures.Add($"Controller is missing the bool '{CarryingParameter}' parameter.");
            }

            bool idleOk = false;
            bool walkOk = false;
            if (controller.layers.Length > 0)
            {
                foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
                {
                    if (child.state.name == "Idle" && child.state.motion == idle)
                    {
                        idleOk = true;
                    }
                    else if (child.state.name == "Walk" && child.state.motion == walk)
                    {
                        walkOk = true;
                    }
                }
            }

            if (!idleOk)
            {
                failures.Add("Controller has no 'Idle' state bound to the Idle_Loop clip.");
            }

            if (!walkOk)
            {
                failures.Add("Controller has no 'Walk' state bound to the Walk_Loop clip.");
            }
        }

        private static void ValidatePrefab(string id, AnimatorController controller, List<string> failures)
        {
            string prefabPath = $"{PrefabFolder}/{id}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing NPC prefab: {prefabPath}");
                return;
            }

            Transform model = prefab.transform.Find("Model");
            if (model == null)
            {
                failures.Add($"{prefabPath} has no Model child.");
                return;
            }

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                failures.Add($"{prefabPath} has no Animator on Model.");
            }
            else
            {
                if (animator.avatar == null || !animator.avatar.isHuman)
                {
                    failures.Add($"{prefabPath} Animator has no humanoid avatar.");
                }

                if (controller != null && animator.runtimeAnimatorController != controller)
                {
                    failures.Add($"{prefabPath} Animator does not use {ControllerPath}.");
                }

                if (animator.applyRootMotion)
                {
                    failures.Add($"{prefabPath} Animator has applyRootMotion enabled.");
                }

                if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                {
                    failures.Add($"{prefabPath} Animator is not set to AlwaysAnimate.");
                }
            }

            if (prefab.GetComponent<NpcLocomotion>() == null)
            {
                failures.Add($"{prefabPath} has no NpcLocomotion.");
            }

            if (prefab.GetComponent<CarryPose>() == null)
            {
                failures.Add($"{prefabPath} has no CarryPose.");
            }

            CarrySocket socket = prefab.GetComponent<CarrySocket>();
            if (socket == null)
            {
                failures.Add($"{prefabPath} has no CarrySocket.");
            }

            Transform anchor = prefab.transform.Find("CarryAnchor");
            if (anchor == null)
            {
                failures.Add($"{prefabPath} has no CarryAnchor child.");
            }
            else if (Vector3.Distance(anchor.localPosition, CarryAnchorLocalPosition) > 0.01f)
            {
                failures.Add($"{prefabPath} CarryAnchor is at {anchor.localPosition}, expected {CarryAnchorLocalPosition}.");
            }
            else if (socket != null && socket.Anchor != anchor)
            {
                failures.Add($"{prefabPath} CarrySocket does not point at CarryAnchor.");
            }

            if (prefab.GetComponentInChildren<NpcPose>(true) != null)
            {
                failures.Add($"{prefabPath} still has the legacy NpcPose component.");
            }
        }

        private static void ValidateScene(List<string> failures)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            FoldingStationBounds(out Bounds tableBounds, out bool hasBounds);

            int customers = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (PickupCustomer customer in root.GetComponentsInChildren<PickupCustomer>(true))
                {
                    customers++;
                    var serialized = new SerializedObject(customer);
                    SerializedProperty stand = serialized.FindProperty("_tableStand");
                    if (stand == null)
                    {
                        failures.Add($"{customer.name} has no _tableStand field.");
                        continue;
                    }

                    Vector3 point = stand.vector3Value;
                    if (Vector3.Distance(point, TableStandPoint) > 0.01f)
                    {
                        failures.Add($"{customer.name} stand point is {point}, expected {TableStandPoint}.");
                    }

                    if (hasBounds
                        && point.x >= tableBounds.min.x && point.x <= tableBounds.max.x
                        && point.z >= tableBounds.min.z && point.z <= tableBounds.max.z)
                    {
                        failures.Add($"{customer.name} stand point {point} is inside the folding table footprint.");
                    }
                }
            }

            if (customers == 0)
            {
                // Phase 11 removed the hidden-room PickupCustomer on purpose; the
                // order customer now uses NPC_OrderCustomer instead.
                Debug.Log("[NPC] No legacy PickupCustomer in the scene; stand-point check skipped.");
            }
        }

        private static void FoldingStationBounds(out Bounds bounds, out bool hasBounds)
        {
            bounds = default;
            hasBounds = false;

            FoldingStation folding = UnityEngine.Object.FindFirstObjectByType<FoldingStation>(FindObjectsInactive.Include);
            if (folding == null)
            {
                return;
            }

            foreach (Renderer renderer in folding.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsUnderLaundryItem(renderer.transform))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        private static bool IsUnderLaundryItem(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (current.GetComponent<LaundryItem>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
