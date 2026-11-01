using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Repairs the two NPC variants whose MakeHuman materials were lost on export
    /// (Unity substituted the default grey "Lit" material) and mounts the
    /// first-person hands on the camera at a distance the near clip plane allows.
    /// </summary>
    public static class CharacterAppearanceFixTool
    {
        private const string Materials = "Assets/LaundryLegends/Art/Materials";
        private const string NpcTextures = "Assets/LaundryLegends/Art/Textures/NPC";
        private const string AttendantPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Attendant.prefab";
        private const string CustomerPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Customer.prefab";
        private const string PlayerPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/Player.prefab";

        private const string StandardShader = "Universal Render Pipeline/Lit";

        [MenuItem("Laundry Legends/Phase 8/Fix NPC Skin and Hands")]
        public static void Fix()
        {
            var log = new StringBuilder();
            log.AppendLine("[APPEARANCE] restore NPC materials");

            Material skinAttendant = EnsureMaterial(
                "MAT_NPC_Skin_Attendant",
                $"{NpcTextures}/middleage_lightskinned_male_diffuse2.png",
                null,
                0.28f);

            Material skinCustomer = EnsureMaterial(
                "MAT_NPC_Skin_Customer",
                $"{NpcTextures}/young_lightskinned_female_diffuse3.png",
                null,
                0.28f);

            Material suitCustomer = EnsureMaterial(
                "MAT_NPC_Suit_Customer",
                $"{NpcTextures}/female_casualsuit02_diffuse.png",
                $"{NpcTextures}/female_casualsuit02_normal.png",
                0.2f);

            Material shoesAttendant = EnsureMaterial(
                "MAT_NPC_Shoes_Attendant",
                $"{NpcTextures}/shoes04_diffuse.png",
                $"{NpcTextures}/shoes04_normal.png",
                0.35f);

            Material shoesCustomer = EnsureMaterial(
                "MAT_NPC_Shoes_Customer",
                $"{NpcTextures}/shoes06_diffuse.png",
                null,
                0.35f);

            Material hairAttendant = EnsureMaterial(
                "MAT_NPC_Hair_Attendant",
                $"{NpcTextures}/short04_diffuse.png",
                null,
                0.25f);

            Material hairCustomer = EnsureMaterial(
                "MAT_NPC_Hair_Customer",
                $"{NpcTextures}/ponytail01_diffuse.png",
                null,
                0.25f);

            Apply(AttendantPrefab, log, new Dictionary<string, Material>
            {
                { "attendant_humanMesh", skinAttendant },
                { "shoes04Mesh", shoesAttendant },
                { "short04Mesh", hairAttendant }
            });

            Apply(CustomerPrefab, log, new Dictionary<string, Material>
            {
                { "customer_humanMesh", skinCustomer },
                { "female_casualsuit02Mesh", suitCustomer },
                { "shoes06Mesh", shoesCustomer },
                { "ponytail01Mesh", hairCustomer }
            });

            FixHands(log);
            FixNormalMaps(log);
            FixNpcColliders(log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            log.AppendLine("[APPEARANCE] done");
            Debug.Log(log.ToString());
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllText("TestResults/appearance-fix.txt", log.ToString());
        }

        private static void Apply(string prefabPath, StringBuilder log, Dictionary<string, Material> byRendererName)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    if (!byRendererName.TryGetValue(renderer.name, out Material material) || material == null)
                    {
                        continue;
                    }

                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }

                    renderer.sharedMaterials = materials;
                    log.AppendLine($"[APPEARANCE] {System.IO.Path.GetFileName(prefabPath)}: '{renderer.name}' -> {material.name}");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static Material EnsureMaterial(string name, string diffusePath, string normalPath, float smoothness)
        {
            string path = $"{Materials}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader lit = Shader.Find(StandardShader);

            if (material == null)
            {
                material = new Material(lit) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (lit != null && material.shader != lit)
            {
                // An existing asset with the wrong shader would silently ignore every
                // _BaseMap/_BaseColor write and stay grey.
                material.shader = lit;
            }

            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            if (diffuse == null)
            {
                Debug.LogWarning($"[APPEARANCE] missing texture {diffusePath}");
            }

            material.SetTexture("_BaseMap", diffuse);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);

            if (!string.IsNullOrEmpty(normalPath))
            {
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void FixNormalMaps(StringBuilder log)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { NpcTextures }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("_normal.png", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType == TextureImporterType.NormalMap)
                {
                    continue;
                }

                // A normal map sampled as sRGB looks flat or wrong under URP.
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
                log.AppendLine($"[APPEARANCE] normal map fixed: {System.IO.Path.GetFileName(path)}");
            }
        }

        /// <summary>
        /// Reverts the experimental collision-based NPC movement: a Character Controller
        /// snags customers in the doorway, so the original trigger collider is restored.
        /// Clipping through furniture needs a navigation solution, not a controller.
        /// </summary>
        private static void FixNpcColliders(StringBuilder log)
        {
            string[] prefabs =
            {
                AttendantPrefab,
                CustomerPrefab,
                "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Regular.prefab"
            };

            foreach (string path in prefabs)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (CharacterController controller in contents.GetComponentsInChildren<CharacterController>(true))
                    {
                        Object.DestroyImmediate(controller);
                    }

                    foreach (Collider collider in contents.GetComponentsInChildren<Collider>(true))
                    {
                        collider.enabled = true;
                    }

                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    log.AppendLine($"[APPEARANCE] colliders restored: {System.IO.Path.GetFileName(path)}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        private static void FixHands(StringBuilder log)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                Transform hands = null;
                Transform camera = null;

                foreach (Transform child in contents.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "FirstPersonHands")
                    {
                        hands = child;
                    }
                    else if (child.name == "PlayerCamera")
                    {
                        camera = child;
                    }
                }

                if (hands == null || camera == null)
                {
                    log.AppendLine($"[APPEARANCE] hands={hands != null} camera={camera != null} - cannot place");
                    return;
                }

                log.AppendLine($"[APPEARANCE] hands parent was '{hands.parent?.name}' at local {hands.localPosition}");

                // The hand geometry is not centred on its transform root, so place it by
                // measuring: reset the root, find where the mesh actually lands relative to
                // the camera, then offset the root so the mesh sits in front of the eyes.
                hands.SetParent(camera, false);
                hands.localPosition = Vector3.zero;
                hands.localRotation = Quaternion.identity;
                hands.localScale = Vector3.one;
                Physics.SyncTransforms();

                var bounds = new Bounds();
                bool hasBounds = false;
                foreach (Renderer renderer in hands.GetComponentsInChildren<Renderer>(true))
                {
                    Vector3 centre = renderer.transform.TransformPoint(renderer.localBounds.center);
                    if (!hasBounds)
                    {
                        bounds = new Bounds(centre, renderer.localBounds.size);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(new Bounds(centre, renderer.localBounds.size));
                    }
                }

                if (!hasBounds)
                {
                    log.AppendLine("[APPEARANCE] no hand renderers found");
                    return;
                }

                Vector3 meshRelativeToCamera = camera.InverseTransformPoint(bounds.center);
                var desired = new Vector3(0f, -0.16f, 0.40f);
                Vector3 offset = desired - meshRelativeToCamera;
                hands.localPosition = offset;
                Physics.SyncTransforms();

                log.AppendLine($"[APPEARANCE] hand mesh offset from root {meshRelativeToCamera}, hands local set to {offset}");
                log.AppendLine($"[APPEARANCE] hand size {bounds.size}");

                foreach (Renderer renderer in hands.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.enabled = true;
                    log.AppendLine($"[APPEARANCE] hand renderer '{renderer.name}' enabled bounds={renderer.bounds.center} size={renderer.bounds.size}");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefab);
                log.AppendLine($"[APPEARANCE] hands mounted on camera at local {hands.localPosition}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}



