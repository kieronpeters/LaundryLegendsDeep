using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Dresses the storefront street with the authored props and corrects the
    /// over-glossy ground materials. Additive and re-runnable; the props live
    /// under a single "StreetDressing" root.
    /// </summary>
    public static class StreetDressingTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string ModelFolder = "Assets/LaundryLegends/Art/Models/Street";

        [MenuItem("Laundry Legends/Phase 7/Dress Street")]
        public static void Dress()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("StreetDressing");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject("StreetDressing");

            // Street lamps: warm pools on the pavement (brief's highest-impact placement).
            Lamp(root.transform, "StreetLamp_A", new Vector3(-5.2f, 0f, -7.85f));
            Lamp(root.transform, "StreetLamp_B", new Vector3(5.6f, 0f, -7.85f));

            // Seating and greenery either side of the entrance.
            Place(root.transform, "Bench.fbx", "Bench_Street", new Vector3(-2.4f, 0f, -7.05f), 0f);
            Place(root.transform, "PottedPlant.fbx", "Plant_StreetLeft", new Vector3(-6.4f, 0f, -6.75f), 0f);
            Place(root.transform, "PottedPlant.fbx", "Plant_StreetRight", new Vector3(3.3f, 0f, -6.85f), 0f);

            // Interior waiting comfort.
            Place(root.transform, "StackChair.fbx", "Chair_Waiting_A", new Vector3(4.85f, 0f, -4.55f), 200f);
            Place(root.transform, "StackChair.fbx", "Chair_Waiting_B", new Vector3(5.45f, 0f, -4.95f), 215f);

            // Curb runs along the pavement edge, road markings beyond it.
            for (int i = -1; i <= 1; i++)
            {
                Place(root.transform, "CurbStrip.fbx", $"Curb_{i + 1}", new Vector3(i * 3.97f, 0f, -8.1f), 0f);
            }

            Place(root.transform, "RoadMarking.fbx", "Marking_A", new Vector3(0f, 0.012f, -9.6f), 0f);
            Place(root.transform, "RoadMarking.fbx", "Marking_B", new Vector3(0f, 0.012f, -11.6f), 0f);

            FixGroundGloss();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[STREET] Dressing applied: 2 lamps, bench, 2 plants, 2 chairs, 3 curbs, 2 markings.");
        }

        private static void Lamp(Transform parent, string name, Vector3 position)
        {
            GameObject lamp = Place(parent, "StreetLamp.fbx", name, position, 0f, false);
            Bounds lampBounds = GetBounds(lamp);

            var lightObject = new GameObject($"{name}_Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = new Vector3(
                lampBounds.center.x,
                lampBounds.max.y - 0.25f,
                lampBounds.center.z);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.82f, 0.58f);
            light.shadows = LightShadows.None;
        }

        /// <summary>
        /// Instantiates an authored model and keeps its import rotation. Every
        /// street FBX imports with a -90 degree X root (Z-up source), so replacing
        /// the instance rotation with a yaw alone laid the props on the pavement.
        /// The instance is then grounded by its render bounds instead of its pivot.
        /// </summary>
        private static GameObject Place(
            Transform parent,
            string file,
            string name,
            Vector3 position,
            float yRotation,
            bool save = true)
        {
            string path = $"{ModelFolder}/{file}";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                throw new System.InvalidOperationException($"Missing street model: {path}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = name;

            Quaternion importRotation = instance.transform.localRotation;
            instance.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f) * importRotation;
            instance.transform.localPosition = position;
            Ground(instance, position.y);
            return instance;
        }

        /// <summary>Moves the instance up or down so its lowest rendered point sits at groundY.</summary>
        private static void Ground(GameObject instance, float groundY)
        {
            Bounds bounds = GetBounds(instance);
            if (bounds.size.y <= 0.0001f)
            {
                return;
            }

            Vector3 position = instance.transform.position;
            instance.transform.position = new Vector3(
                position.x,
                position.y + (groundY - bounds.min.y),
                position.z);
        }

        private static Bounds GetBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(instance.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void FixGroundGloss()
        {
            SetSmoothness("Assets/LaundryLegends/Art/Materials/MAT_Sidewalk.mat", 0.15f);
            SetSmoothness("Assets/LaundryLegends/Art/Materials/MAT_Asphalt.mat", 0.10f);
            SetSmoothness("Assets/LaundryLegends/Art/Materials/MAT_FloorTile.mat", 0.22f);
        }

        private static void SetSmoothness(string path, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Debug.LogWarning($"[STREET] Missing material {path}");
                return;
            }

            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
        }
    }
}
