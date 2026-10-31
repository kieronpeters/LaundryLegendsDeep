using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Tests alternate import settings on a copy of the original NPC model to
    /// find a combination where the humanoid skeleton lands near the origin.
    /// </summary>
    public static class ImportSettingsProbe
    {
        private const string Path = "Assets/LaundryLegends/Art/Models/NPC/_Test/NPC_Attendant_Orig.fbx";

        [MenuItem("Laundry Legends/Phase 4/Probe Import Settings")]
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
                Debug.LogError($"[PROBE3] no importer for {Path}");
                return;
            }

            Try(importer, "noFileScale_scale1", false, 1f);
            Try(importer, "noFileScale_scale15.5", false, 15.5f);
            Try(importer, "fileScale_scale1", true, 1f);
        }

        private static void Try(ModelImporter importer, string label, bool useFileScale, float globalScale)
        {
            importer.useFileScale = useFileScale;
            importer.globalScale = globalScale;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();

            Avatar avatar = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Path))
            {
                if (asset is Avatar candidate)
                {
                    avatar = candidate;
                    break;
                }
            }

            if (avatar == null)
            {
                Debug.Log($"[PROBE3] {label}: NO AVATAR");
                return;
            }

            string hips = "none";
            foreach (SkeletonBone bone in avatar.humanDescription.skeleton)
            {
                if (bone.name.Contains("Hips"))
                {
                    hips = bone.position.ToString();
                    break;
                }
            }

            Debug.Log($"[PROBE3] {label}: human={avatar.isHuman} valid={avatar.isValid} hips={hips}");
        }
    }
}
