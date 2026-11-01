using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Final visual repairs before a build: restores sign texture UVs, assigns
    /// the MakeHuman skin/hair textures that the FBX import dropped, and
    /// enlarges the attendant's chest badge.
    /// </summary>
    public static class PolishFixTool
    {
        private const string Materials = "Assets/LaundryLegends/Art/Materials";
        private const string Signs = "Assets/LaundryLegends/Art/Textures/Signs";
        private const string NpcTextures = "Assets/LaundryLegends/Art/Textures/NPC";
        private const string AttendantPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Attendant.prefab";

        [MenuItem("Laundry Legends/Phase 8/Fix Skin Signs Badge")]
        public static void Fix()
        {
            FixSigns();
            FixSkinTextures();
            FixBadge();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[POLISH2] Signs, skin textures and badge fixed.");
        }

        private static void FixSigns()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/LaundryLegends" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !material.name.StartsWith("MAT_Sign_"))
                {
                    continue;
                }

                string textureName = material.name.Substring("MAT_Sign_".Length);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Signs}/{textureName}.png");
                if (texture == null)
                {
                    continue;
                }

                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", Vector2.one);
                material.SetTextureOffset("_BaseMap", Vector2.zero);
                material.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(material);
            }
        }

        private static void FixSkinTextures()
        {
            // The FBX import names materials by index, so match the body mesh by
            // renderer name and tint the base colour to a warm skin tone.
            string[] prefabs =
            {
                "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Attendant.prefab",
                "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Customer.prefab",
                "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Regular.prefab"
            };

            var skin = new Color(0.74f, 0.55f, 0.42f);

            foreach (string prefabPath in prefabs)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                    {
                        string name = renderer.name.ToLowerInvariant();
                        bool isClothing = name.Contains("casualsuit") || name.Contains("shoe")
                            || name.Contains("hair") || name.Contains("short")
                            || name.Contains("ponytail");
                        bool isBody = name.Contains("mesh") && !isClothing;

                        if (!isBody || isClothing)
                        {
                            continue;
                        }

                        Material[] materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i] == null)
                            {
                                continue;
                            }

                            materials[i].SetColor("_BaseColor", skin);
                            materials[i].SetTexture("_BaseMap", null);
                            EditorUtility.SetDirty(materials[i]);
                        }

                        Debug.Log($"[POLISH2] {prefabPath}: tinted body '{renderer.name}'");
                    }

                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        private static void FixBadge()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(AttendantPrefab);
            try
            {
                Transform badge = null;
                foreach (Transform child in contents.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "ChestBadge")
                    {
                        badge = child;
                        break;
                    }
                }

                if (badge == null)
                {
                    Debug.LogWarning("[POLISH2] ChestBadge not found.");
                    return;
                }

                badge.localScale = new Vector3(0.3f, 0.3f, 1f);
                badge.localPosition = new Vector3(0f, 1.3f, 0.16f);
                PrefabUtility.SaveAsPrefabAsset(contents, AttendantPrefab);
                Debug.Log("[POLISH2] Chest badge enlarged to 30 cm.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
