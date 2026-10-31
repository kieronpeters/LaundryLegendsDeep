using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Logs the humanoid avatar mapping for each NPC so we can see whether the
    /// retarget is being fed a sane skeleton (correct Hips/Head/Feet) or a
    /// mis-mapped one.
    /// </summary>
    public static class AvatarMappingDiagnostics
    {
        [MenuItem("Laundry Legends/Phase 4/Diagnose Avatar Mapping")]
        public static void Diagnose()
        {
            foreach (string id in new[] { "NPC_Attendant", "NPC_Customer", "NPC_Regular" })
            {
                string path = $"Assets/LaundryLegends/Art/Models/NPC/{id}.fbx";
                Avatar avatar = null;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Avatar candidate)
                    {
                        avatar = candidate;
                        break;
                    }
                }

                if (avatar == null)
                {
                    Debug.LogError($"[AVATAR] {id}: no avatar sub-asset at all.");
                    continue;
                }

                Debug.Log($"[AVATAR] {id}: name={avatar.name} isHuman={avatar.isHuman} valid={avatar.isValid}");

                HumanDescription description = avatar.humanDescription;
                Debug.Log($"[AVATAR] {id}: skeleton bones={description.skeleton.Length}");

                for (int i = 0; i < description.human.Length; i++)
                {
                    HumanBone bone = description.human[i];
                    if (bone.boneName == "Hips" || bone.boneName == "Head" || bone.boneName == "LeftFoot"
                        || bone.boneName == "RightFoot" || bone.boneName == "Spine")
                    {
                        Debug.Log($"[AVATAR] {id}: {bone.boneName} <- '{bone.humanName}' (limit={bone.limit})");
                    }
                }

                // Print the skeleton entries nearest the origin extremes.
                foreach (SkeletonBone bone in description.skeleton)
                {
                    if (bone.name == "mixamorig:Hips" || bone.name == "Hips")
                    {
                        Debug.Log($"[AVATAR] {id}: skeleton '{bone.name}' position={bone.position} scale={bone.scale}");
                    }
                }
            }
        }
    }
}
