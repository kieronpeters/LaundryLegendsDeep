using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Imports the CC0 Quaternius character as humanoid and reports the avatar
    /// mapping so we know whether Unity accepted the rig before swapping NPCs.
    /// </summary>
    public static class CharacterImportProbe
    {
        private const string Path = "Assets/LaundryLegends/Art/Models/NPC/QuaterniusMan.fbx";

        [MenuItem("Laundry Legends/Phase 4/Probe Character Import")]
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
                Debug.LogError("[CHAR] no importer");
                return;
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
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
                Debug.LogError("[CHAR] no avatar created");
                return;
            }

            Debug.Log($"[CHAR] avatar={avatar.name} human={avatar.isHuman} valid={avatar.isValid}");

            int shown = 0;
            foreach (SkeletonBone bone in avatar.humanDescription.skeleton)
            {
                if (shown++ < 14)
                {
                    Debug.Log($"[CHAR] bone '{bone.name}' position={bone.position}");
                }
            }

            foreach (SkeletonBone bone in avatar.humanDescription.skeleton)
            {
                if (bone.name == "Hips" || bone.name.Contains("Hips"))
                {
                    Debug.Log($"[CHAR] hips position={bone.position}");
                }
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"[CHAR] renderer '{renderer.name}' materials={renderer.sharedMaterials.Length} bounds={renderer.bounds.size}");
            }
        }
    }
}
