using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Applies the authored art pass: sign panels, two decals, and the new PBR
    /// albedo maps on the main surfaces. Idempotent and re-runnable.
    /// </summary>
    public static class ArtIntegrationTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string Pbr = "Assets/LaundryLegends/Art/Textures/PBR";
        private const string Decals = "Assets/LaundryLegends/Art/Textures/Decals";
        private const string Materials = "Assets/LaundryLegends/Art/Materials";

        [MenuItem("Laundry Legends/Phase 7/Apply Art Pass")]
        public static void Apply()
        {
            SignageArtTool.Apply();
            AssignPbr();
            ApplyDecals();
            Debug.Log("[ART] Sign panels, PBR albedo maps and decals applied.");
        }

        private static void AssignPbr()
        {
            Assign("MAT_FloorTile.mat", "FloorTile_Color.jpg", new Vector2(8f, 6f), 0.22f);
            Assign("MAT_WallPaint.mat", "Plaster_Color.jpg", new Vector2(6f, 3f), 0.18f);
            Assign("MAT_CounterWood.mat", "Wood_Color.jpg", new Vector2(2f, 1f), 0.28f);
            Assign("MAT_Sidewalk.mat", "Concrete_Color.jpg", new Vector2(6f, 3f), 0.15f);
            Assign("MAT_Asphalt.mat", "Asphalt_Color.jpg", new Vector2(8f, 8f), 0.10f);
        }

        private static void Assign(string materialFile, string textureFile, Vector2 tiling, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/{materialFile}");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Pbr}/{textureFile}");
            if (material == null || texture == null)
            {
                Debug.LogWarning($"[ART] Missing {materialFile} or {textureFile}");
                return;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
        }

        private static void ApplyDecals()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = GameObject.Find("ArtDecals");
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            root = new GameObject("ArtDecals");

            Decal(root.transform, "FloorGrime", "02_floor_grime/floorgrime_edge_color.png",
                new Vector3(0f, 0.006f, 4.15f), Quaternion.Euler(90f, 0f, 0f), new Vector3(9f, 1.3f, 1f));

            Decal(root.transform, "WallDamp", "01_water_stain/waterstain_damp_color.png",
                new Vector3(1.6f, 1.35f, 5.2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.2f, 1.6f, 1f));

            Decal(root.transform, "WallDamp2", "01_water_stain/waterstain_broad_color.png",
                new Vector3(-2.4f, 1.5f, 5.2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.6f, 1.8f, 1f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Decal(Transform parent, string name, string texturePath, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var material = EnsureDecalMaterial(name, texturePath);
            if (material == null)
            {
                return;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.position = position;
            quad.transform.rotation = rotation;
            quad.transform.localScale = scale;
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material EnsureDecalMaterial(string name, string texturePath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Decals}/{texturePath}");
            if (texture == null)
            {
                Debug.LogWarning($"[ART] Missing decal {texturePath}");
                return null;
            }

            string path = $"{Materials}/MAT_Decal_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
