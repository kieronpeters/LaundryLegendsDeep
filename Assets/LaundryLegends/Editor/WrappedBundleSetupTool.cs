using LaundryLegends.Laundry;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Imports the authored wrapped-bundle FBX, builds its cloth/film
    /// materials, makes a prefab, and assigns it to the laundry item so the
    /// Wrapped stage shows a real bag instead of a transparent cube.
    /// </summary>
    public static class WrappedBundleSetupTool
    {
        private const string ModelPath = "Assets/LaundryLegends/Art/Models/WrappedBundle.fbx";
        private const string PrefabPath = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/WrappedBundle.prefab";
        private const string LaundryPrefabPath = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab";
        private const string ClothMaterialPath = "Assets/LaundryLegends/Art/Materials/MAT_BundleCloth.mat";
        private const string FilmMaterialPath = "Assets/LaundryLegends/Art/Materials/MAT_BundleFilm.mat";

        [MenuItem("Laundry Legends/Phase 4/Apply Wrapped Bundle")]
        public static void ApplyWrappedBundle()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer != null)
            {
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.addCollider = false;
                importer.SaveAndReimport();
            }

            Material cloth = EnsureOpaque(ClothMaterialPath, new Color(0.11f, 0.28f, 0.34f));
            Material film = EnsureTransparent(FilmMaterialPath, new Color(0.82f, 0.90f, 0.96f, 0.16f));

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new System.InvalidOperationException($"Missing bundle model at {ModelPath}");
            }

            var instance = Object.Instantiate(model);
            instance.name = "WrappedBundle";
            try
            {
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    bool isFilm = renderer.name.IndexOf("Film", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    renderer.sharedMaterial = isFilm ? film : cloth;
                    Debug.Log($"[BUNDLE] {renderer.name} -> {(isFilm ? "film" : "cloth")}");
                }

                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }

            AssignToLaundryPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BUNDLE] Wrapped bundle applied.");
        }

        private static void AssignToLaundryPrefab()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(LaundryPrefabPath);
            try
            {
                LaundryItem item = prefab.GetComponent<LaundryItem>();
                var bundle = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (item == null || bundle == null)
                {
                    throw new System.InvalidOperationException("LaundryItem or bundle prefab missing.");
                }

                SerializedObject so = new SerializedObject(item);
                SerializedProperty property = so.FindProperty("_wrappedVisualPrefab");
                property.objectReferenceValue = bundle;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefab, LaundryPrefabPath);
                Debug.Log("[BUNDLE] LooseLaundry now references the bundle visual.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static Material EnsureOpaque(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Standard");
            material.color = color;
            material.SetFloat("_Glossiness", 0.1f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureTransparent(string path, Color color)
        {
            Material material = EnsureOpaque(path, color);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            material.SetFloat("_Glossiness", 0.55f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
