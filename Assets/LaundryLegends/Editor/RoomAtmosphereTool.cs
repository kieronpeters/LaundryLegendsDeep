using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// First room-realism pass: fixtures, sane light falloff, floor tiling, and
    /// contact shadows. Everything is additive and idempotent so it can be
    /// re-run after other room edits.
    /// </summary>
    public static class RoomAtmosphereTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string MaterialsFolder = "Assets/LaundryLegends/Art/Materials";

        [MenuItem("Laundry Legends/Phase 4/Apply Room Atmosphere")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                TuneLights(root.transform);
                AddFixtures(root.transform);
                AddContactShadows(root.transform);
                TuneTiling();
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LiftAmbient();
            Debug.Log("[ROOM] Atmosphere applied.");
        }

        /// <summary>
        /// The scene's flat ambient of 0.24 leaves the ceiling black once the
        /// point-light range is tightened; lift it slightly at the scene level.
        /// </summary>
        private static void LiftAmbient()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/LaundryLegends/Scenes/Laundromat.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.36f, 0.36f, 0.38f);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[ROOM] Ambient lifted to 0.36.");
        }

        private static void TuneLights(Transform root)
        {
            int tuned = 0;
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type != LightType.Point)
                {
                    continue;
                }

                light.range = 7.5f;
                light.intensity = 2.2f;
                light.color = new Color(1f, 0.9f, 0.76f);
                light.shadows = tuned < 2 ? LightShadows.Soft : LightShadows.None;
                tuned++;
            }

            Debug.Log($"[ROOM] Tuned {tuned} point lights (range 6.5, warm).");
        }

        private static void AddFixtures(Transform root)
        {
            Transform existing = root.Find("CeilingFixtures");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var fixtures = new GameObject("CeilingFixtures");
            fixtures.transform.SetParent(root, false);

            Material tube = EnsureEmissiveMaterial(
                $"{MaterialsFolder}/MAT_CeilingTube.mat",
                new Color(1f, 0.96f, 0.86f),
                2.4f);

            Vector3[] positions =
            {
                new Vector3(0f, 3.15f, 3.6f),
                new Vector3(-3.4f, 3.15f, 0.4f),
                new Vector3(3.4f, 3.15f, 0.4f),
                new Vector3(0f, 3.15f, -2.8f)
            };

            foreach (Vector3 position in positions)
            {
                var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fixture.name = "CeilingTube";
                Object.DestroyImmediate(fixture.GetComponent<Collider>());
                fixture.transform.SetParent(fixtures.transform, false);
                fixture.transform.localPosition = position;
                fixture.transform.localScale = new Vector3(1.6f, 0.06f, 0.16f);
                fixture.GetComponent<Renderer>().sharedMaterial = tube;

                var lightObject = new GameObject("TubeLight");
                lightObject.transform.SetParent(fixtures.transform, false);
                lightObject.transform.localPosition = position + new Vector3(0f, -0.25f, 0f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 7f;
                light.intensity = 1.5f;
                light.color = new Color(1f, 0.93f, 0.82f);
                light.shadows = LightShadows.None;
            }

            Debug.Log($"[ROOM] Added {positions.Length} ceiling fixtures.");
        }

        private static void AddContactShadows(Transform root)
        {
            Transform existing = root.Find("ContactShadows");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var shadows = new GameObject("ContactShadows");
            shadows.transform.SetParent(root, false);

            Material material = EnsureGrimeMaterial($"{MaterialsFolder}/MAT_ContactShadow.mat");

            AddQuad(shadows.transform, material, "Shadow_Machines", new Vector3(0f, 0.012f, 4.6f), new Vector3(9.4f, 1.4f));
            AddQuad(shadows.transform, material, "Shadow_Counter", new Vector3(-4.5f, 0.012f, -2.5f), new Vector3(3.0f, 1.4f));
            AddQuad(shadows.transform, material, "Shadow_Table", new Vector3(4.5f, 0.012f, 1.0f), new Vector3(3.2f, 1.6f));

            Debug.Log("[ROOM] Added contact shadow patches.");
        }

        private static void AddQuad(Transform parent, Material material, string name, Vector3 position, Vector2 size)
        {
            // Unity's built-in plane is 10x10 units, so scale by size/10.
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = position;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(size.x / 10f, size.y / 10f, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void TuneTiling()
        {
            SetTiling($"{MaterialsFolder}/MAT_FloorTile.mat", new Vector2(8f, 6f));
            SetTiling($"{MaterialsFolder}/MAT_WallPaint.mat", new Vector2(6f, 3f));
            SetTiling($"{MaterialsFolder}/MAT_CounterWood.mat", new Vector2(2f, 1f));
        }

        private static void SetTiling(string path, Vector2 tiling)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Debug.LogWarning($"[ROOM] Missing material {path}");
                return;
            }

            material.mainTextureScale = tiling;
            EditorUtility.SetDirty(material);
        }

        private static Material EnsureEmissiveMaterial(string path, Color color, float emission)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureGrimeMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = new Color(0.05f, 0.04f, 0.035f, 0.35f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
