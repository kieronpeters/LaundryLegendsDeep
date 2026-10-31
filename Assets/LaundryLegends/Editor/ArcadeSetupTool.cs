using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Places the arcade cabinet in the laundromat, builds its screen quad and
    /// material, and wires the maze view and the coin/cost interaction.
    /// </summary>
    public static class ArcadeSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string ModelPath = "Assets/LaundryLegends/Art/Models/Arcade/ArcadeCabinet.fbx";
        private const string MaterialPath = "Assets/LaundryLegends/Art/Materials/MAT_ArcadeScreen.mat";

        [MenuItem("Laundry Legends/Phase 8/Place Arcade Cabinet")]
        public static void Place()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("ArcadeCabinet");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new System.InvalidOperationException($"Missing cabinet model at {ModelPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "ArcadeCabinet";
            instance.transform.position = new Vector3(5.9f, 0f, 2.4f);
            instance.transform.rotation = Quaternion.Euler(0f, 200f, 0f);

            // Screen quad sits on the authored ScreenSurface anchor.
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "ScreenQuad";
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            screen.transform.SetParent(instance.transform, false);
            screen.transform.localPosition = new Vector3(0f, 1.2403f, 0.1946f);
            screen.transform.localRotation = Quaternion.Euler(-9.45f, 0f, 0f);
            screen.transform.localScale = new Vector3(0.5f, 0.4f, 1f);

            Material material = EnsureScreenMaterial();
            screen.GetComponent<Renderer>().sharedMaterial = material;

            var view = instance.GetComponent<MazeGameView>();
            if (view == null)
            {
                view = instance.AddComponent<MazeGameView>();
            }

            view.Configure(screen.GetComponent<Renderer>());

            var cabinet = instance.GetComponent<ArcadeCabinet>();
            if (cabinet == null)
            {
                cabinet = instance.AddComponent<ArcadeCabinet>();
            }

            var spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            cabinet.Configure(view, spawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARCADE] Cabinet placed at (5.9, 0, 2.4) with screen quad and maze view.");
        }

        [MenuItem("Laundry Legends/Phase 8/Validate Arcade Cabinet")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject cabinet = GameObject.Find("ArcadeCabinet");
            if (cabinet == null)
            {
                throw new System.InvalidOperationException("ArcadeCabinet missing from the scene.");
            }

            if (cabinet.GetComponent<ArcadeCabinet>() == null || cabinet.GetComponent<MazeGameView>() == null)
            {
                throw new System.InvalidOperationException("Cabinet is missing its game components.");
            }

            if (cabinet.transform.Find("ScreenQuad") == null)
            {
                throw new System.InvalidOperationException("Cabinet has no ScreenQuad.");
            }

            Debug.Log("[ARCADE] Validation passed: cabinet, screen quad, view, and game component present.");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static Material EnsureScreenMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = Shader.Find("Standard");
            material.color = Color.white;
            material.SetFloat("_Glossiness", 0.05f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.25f, 0.25f, 0.3f));
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
