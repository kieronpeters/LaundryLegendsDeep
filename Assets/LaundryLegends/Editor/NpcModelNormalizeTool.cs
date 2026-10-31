using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Re-imports the normalised NPC models at scale 1, restores their
    /// materials, and rebuilds the animation wiring. The original FBX files
    /// were authored tiny and compensated with a large importer scale, which
    /// broke humanoid retargeting.
    /// </summary>
    public static class NpcModelNormalizeTool
    {
        private static readonly string[] Ids = { "NPC_Attendant", "NPC_Customer", "NPC_Regular" };

        [MenuItem("Laundry Legends/Phase 4/Normalize NPC Models")]
        public static void NormalizeNpcModels()
        {
            foreach (string id in Ids)
            {
                string modelPath = $"Assets/LaundryLegends/Art/Models/NPC/{id}.fbx";
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null)
                {
                    throw new System.InvalidOperationException($"Missing importer for {modelPath}");
                }

                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.addCollider = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.SaveAndReimport();

                Debug.Log($"[NORM] {id}: importer scale reset to 1 and reimported.");
            }

            AssetDatabase.Refresh();

            foreach (string id in Ids)
            {
                RestoreMaterials(id);
            }

            NpcAnimationSetupTool.BuildNpcAnimation();
            ReportHeights();
            Debug.Log("[NORM] NPC models normalised.");
        }

        private static void RestoreMaterials(string id)
        {
            string prefabPath = $"Assets/LaundryLegends/Prefabs/Gameplay/NPC/{id}.prefab";
            var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/LaundryLegends/Art/Materials/MAT_{id}.mat");
            if (material == null)
            {
                Debug.LogWarning($"[NORM] {id}: missing MAT_{id}.mat; leaving materials as imported.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var applied = 0;
                foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                    {
                        renderer.sharedMaterial = material;
                        applied++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log($"[NORM] {id}: material reassigned to {applied} renderer(s).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ReportHeights()
        {
            foreach (string id in Ids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LaundryLegends/Prefabs/Gameplay/NPC/{id}.prefab");
                if (prefab == null)
                {
                    continue;
                }

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    continue;
                }

                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                Debug.Log($"[NORM] {id}: bounds center={bounds.center} size={bounds.size}");
            }
        }
    }
}
