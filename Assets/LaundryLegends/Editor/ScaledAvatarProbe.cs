using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Tests whether Unity can build a humanoid avatar from the rescaled NPC
    /// export at a clean asset path. Reports the avatar and its hip position.
    /// </summary>
    public static class ScaledAvatarProbe
    {
        private const string Path = "Assets/LaundryLegends/Art/Models/NPC/_Test/NPC_Attendant_Scaled.fbx";

        [MenuItem("Laundry Legends/Phase 4/Probe Scaled Avatar")]
        public static void Probe()
        {
            var importer = AssetImporter.GetAtPath(Path) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(Path) as ModelImporter;
            }

            if (importer == null)
            {
                Debug.LogError($"[PROBE] no importer for {Path}");
                return;
            }

            importer.globalScale = 1f;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Path))
            {
                if (asset is Avatar avatar)
                {
                    Debug.Log($"[PROBE] avatar={avatar.name} isHuman={avatar.isHuman} valid={avatar.isValid}");
                    foreach (SkeletonBone bone in avatar.humanDescription.skeleton)
                    {
                        if (bone.name.Contains("Hips"))
                        {
                            Debug.Log($"[PROBE] hips position={bone.position} scale={bone.scale}");
                        }
                    }
                }
                else if (asset is GameObject go)
                {
                    Debug.Log($"[PROBE] model root '{go.name}' children={go.transform.childCount}");
                }
            }
        }
    }
}
