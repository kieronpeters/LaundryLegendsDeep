using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Ordered, evidence-producing attempts to get a humanoid avatar for the
    /// clothed CC0 Quaternius character.
    ///
    /// The importer refuses the rig (avatar.isHuman == false) and the first
    /// AvatarBuilder attempt returned valid=False. These methods run each
    /// candidate fix in sequence, capture every Unity log line emitted while
    /// building, and append the outcome to TestResults/npc-recovery-attempts.txt
    /// so the report quotes exact messages rather than guesses.
    ///
    /// Batch usage:
    /// Unity.exe -batchmode -quit -nographics -projectPath "PROJECT" ^
    ///   -executeMethod LaundryLegends.Editor.NpcHumanoidRecovery.RunAllAttempts ^
    ///   -logFile "TestResults\npc-recovery.log"
    /// </summary>
    public static class NpcHumanoidRecovery
    {
        private const string ModelPath = "Assets/LaundryLegends/Art/Models/NPC/QuaterniusMan.fbx";

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

        /// <summary>
        /// Unity's humanoid solver requires these parent relationships. They are
        /// the structural precondition every BuildHumanAvatar attempt depends on.
        /// </summary>
        private static readonly (string Child, string Ancestor)[] RequiredChain =
        {
            ("Spine", "Hips"),
            ("Chest", "Spine"),
            ("Neck", "Chest"),
            ("Head", "Neck"),
            ("LeftShoulder", "Chest"),
            ("LeftUpperArm", "LeftShoulder"),
            ("LeftLowerArm", "LeftUpperArm"),
            ("LeftHand", "LeftLowerArm"),
            ("RightShoulder", "Chest"),
            ("RightUpperArm", "RightShoulder"),
            ("RightLowerArm", "RightUpperArm"),
            ("RightHand", "RightLowerArm"),
            ("LeftUpperLeg", "Hips"),
            ("LeftLowerLeg", "LeftUpperLeg"),
            ("LeftFoot", "LeftLowerLeg"),
            ("RightUpperLeg", "Hips"),
            ("RightLowerLeg", "RightUpperLeg"),
            ("RightFoot", "RightLowerLeg")
        };

        private static readonly List<string> s_log = new List<string>();
        private static StringBuilder s_report;

        public static string ReportPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults", "npc-recovery-attempts.txt"));

        [MenuItem("Laundry Legends/Phase 4/Recovery/Run All Avatar Attempts")]
        public static void RunAllAttempts()
        {
            s_report = new StringBuilder();
            Line($"utc={DateTime.UtcNow:O}");
            Line($"model={ModelPath}");

            Attempt0InspectRig();
            Attempt1BonesOnlySkeleton();
            Attempt2ExplicitLimits();
            Attempt3GenericThenBuild();

            WriteReport();
            Debug.Log($"[RECOVERY] attempts written to {ReportPath}");
        }

        // ------------------------------------------------------------------
        // Attempt 0 - baseline facts (no change)
        // ------------------------------------------------------------------

        [MenuItem("Laundry Legends/Phase 4/Recovery/Attempt 0 - Inspect")]
        public static void Attempt0InspectRig()
        {
            s_report ??= new StringBuilder();
            Line("");
            Line("=== ATTEMPT 0: inspect imported rig ===");

            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            Line($"importer animationType={importer?.animationType} avatarSetup={importer?.avatarSetup} " +
                 $"globalScale={importer?.globalScale} useFileScale={importer?.useFileScale}");

            Avatar avatar = FindAvatar();
            if (avatar == null)
            {
                Line("RESULT: no avatar sub-asset at all (human=false valid=false).");
                return;
            }

            Line($"RESULT: avatar='{avatar.name}' human={avatar.isHuman} valid={avatar.isValid}");
            HumanDescription description = avatar.humanDescription;
            Line($"humanDescription.human={description.human.Length} skeleton={description.skeleton.Length}");

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = UnityEngine.Object.Instantiate(model);
            try
            {
                instance.name = "QuaterniusMan";
                Line("transform tree:");
                DumpTree(instance.transform, 0);

                Line("humanoid chain requirements:");
                foreach ((string child, string ancestor) in RequiredChain)
                {
                    Transform c = Find(instance.transform, child);
                    Transform a = Find(instance.transform, ancestor);
                    bool ok = c != null && a != null && IsDescendantOf(c, a);
                    Line($"  {(ok ? "ok  " : "FAIL")} {child} {(ok ? "is" : "is NOT")} a descendant of {ancestor}" +
                         (c != null ? $" (parent='{c.parent?.name}')" : " (missing)"));
                }

                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Line($"renderer '{renderer.name}' type={renderer.GetType().Name} " +
                         $"materials={renderer.sharedMaterials.Length}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        // ------------------------------------------------------------------
        // Attempt 1 - bones-only skeleton
        // ------------------------------------------------------------------

        [MenuItem("Laundry Legends/Phase 4/Recovery/Attempt 1 - Bones-Only Skeleton")]
        public static void Attempt1BonesOnlySkeleton()
        {
            s_report ??= new StringBuilder();
            Line("");
            Line("=== ATTEMPT 1: BuildHumanAvatar with bones-only skeleton ===");

            TryBuild(useExplicitLimits: false, label: "attempt1");
        }

        // ------------------------------------------------------------------
        // Attempt 2 - explicit limits
        // ------------------------------------------------------------------

        [MenuItem("Laundry Legends/Phase 4/Recovery/Attempt 2 - Explicit Limits")]
        public static void Attempt2ExplicitLimits()
        {
            s_report ??= new StringBuilder();
            Line("");
            Line("=== ATTEMPT 2: BuildHumanAvatar with explicit joint limits ===");

            TryBuild(useExplicitLimits: true, label: "attempt2");
        }

        // ------------------------------------------------------------------
        // Attempt 3 - import as Generic, then build
        // ------------------------------------------------------------------

        [MenuItem("Laundry Legends/Phase 4/Recovery/Attempt 3 - Generic Then Build")]
        public static void Attempt3GenericThenBuild()
        {
            s_report ??= new StringBuilder();
            Line("");
            Line("=== ATTEMPT 3: import as Generic, then BuildHumanAvatar ===");

            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                Line("RESULT: no ModelImporter.");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            Avatar generic = FindAvatar();
            Line($"generic import: avatar={(generic != null ? generic.name : "null")} " +
                 $"human={(generic != null && generic.isHuman)} valid={(generic != null && generic.isValid)} " +
                 $"skeletonBones={(generic != null ? generic.humanDescription.skeleton.Length : -1)}");

            TryBuild(useExplicitLimits: true, label: "attempt3");

            // Leave the importer in the state the pipeline expects for the fallback
            // (Blender re-export) so a later run does not inherit Generic silently.
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
            Line("restored importer: animationType=Human avatarSetup=CreateFromThisModel");
        }

        // ------------------------------------------------------------------
        // Shared builder
        // ------------------------------------------------------------------

        private static void TryBuild(bool useExplicitLimits, string label)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Line("RESULT: model asset missing.");
                return;
            }

            var messages = new List<string>();
            void Handler(string condition, string stackTrace, LogType type)
            {
                messages.Add($"{type}: {condition}");
            }

            var instance = UnityEngine.Object.Instantiate(model);
            Application.LogCallback handler = Handler;
            Application.logMessageReceived += handler;
            try
            {
                instance.name = "QuaterniusMan";

                List<SkeletonBone> skeleton = BuildBonesOnlySkeleton(instance.transform);
                var human = new List<HumanBone>();
                foreach ((string bone, string humanName) in HumanMap)
                {
                    human.Add(new HumanBone
                    {
                        boneName = bone,
                        humanName = humanName,
                        limit = useExplicitLimits
                            ? new HumanLimit
                            {
                                useDefaultValues = false,
                                min = new Vector3(-180f, -180f, -180f),
                                max = new Vector3(180f, 180f, 180f)
                            }
                            : new HumanLimit { useDefaultValues = true }
                    });
                }

                var description = new HumanDescription
                {
                    human = human.ToArray(),
                    skeleton = skeleton.ToArray(),
                    hasTranslationDoF = false
                };

                Line($"{label}: skeleton entries={skeleton.Count} human entries={human.Count} " +
                     $"explicitLimits={useExplicitLimits}");

                Avatar avatar = null;
                string error = null;
                try
                {
                    avatar = AvatarBuilder.BuildHumanAvatar(instance, description);
                }
                catch (Exception exception)
                {
                    error = exception.ToString();
                }

                if (error != null)
                {
                    Line($"{label}: BuildHumanAvatar threw: {error}");
                }
                else if (avatar == null)
                {
                    Line($"{label}: BuildHumanAvatar returned null.");
                }
                else
                {
                    Line($"{label}: BuildHumanAvatar returned '{avatar.name}' " +
                         $"human={avatar.isHuman} valid={avatar.isValid}");
                    HumanDescription built = avatar.humanDescription;
                    Line($"{label}: built description human={built.human.Length} skeleton={built.skeleton.Length}");
                }
            }
            finally
            {
                Application.logMessageReceived -= handler;
                UnityEngine.Object.DestroyImmediate(instance);
            }

            if (messages.Count == 0)
            {
                Line($"{label}: Unity emitted no log messages during the build.");
            }
            else
            {
                Line($"{label}: Unity log messages ({messages.Count}):");
                foreach (string message in messages)
                {
                    Line($"  | {message}");
                }
            }
        }

        /// <summary>
        /// Parent-first list of the root, the armature, and every bone under it.
        /// Mesh transforms (BaseHuman) and helper IK targets are excluded: the
        /// skeleton is a rigid hierarchy, and bones left out of it are still
        /// resolvable by the Avatar as long as their ancestors are present.
        /// </summary>
        private static List<SkeletonBone> BuildBonesOnlySkeleton(Transform root)
        {
            var bones = new List<SkeletonBone>();
            Add(root);
            return bones;

            void Add(Transform transform)
            {
                bones.Add(new SkeletonBone
                {
                    name = transform.name,
                    position = transform.localPosition,
                    rotation = transform.localRotation,
                    scale = transform.localScale
                });

                foreach (Transform child in transform)
                {
                    if (child.GetComponent<Renderer>() != null)
                    {
                        continue;
                    }

                    Add(child);
                }
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static Avatar FindAvatar()
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            return null;
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            for (Transform current = child; current != null; current = current.parent)
            {
                if (current == ancestor)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DumpTree(Transform transform, int depth)
        {
            string flags = transform.GetComponent<Renderer>() != null ? " [MESH]" : string.Empty;
            Line($"{new string(' ', depth * 2)}- {transform.name}{flags} " +
                 $"local={transform.localPosition.ToString("0.000")}");
            foreach (Transform child in transform)
            {
                DumpTree(child, depth + 1);
            }
        }

        private static void Line(string text)
        {
            s_report ??= new StringBuilder();
            s_report.AppendLine(text);
            Debug.Log($"[RECOVERY] {text}");
        }

        private static void WriteReport()
        {
            if (s_report == null)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? ".");
            File.WriteAllText(ReportPath, s_report.ToString());
        }
    }
}
