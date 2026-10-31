using LaundryLegends.Customers;
using LaundryLegends.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the customer lane (waypoints + order slot) and wires the order
    /// spawner into the Laundromat scene. Additive and idempotent; it does not
    /// touch AmbientActors, so the existing ambient NPC tests keep working.
    /// </summary>
    public static class CustomerSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string CustomerPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Customer.prefab";
        private const string LaundryPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab";

        [MenuItem("Laundry Legends/Phase 5/Build Customer Lane")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("CustomerLane");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var lane = new GameObject("CustomerLane");
            Transform street = Point(lane.transform, "Lane_Street", new Vector3(0f, 0f, -9.5f));
            Transform door = Point(lane.transform, "Lane_Door", new Vector3(0f, 0f, -5.6f));
            Transform counter = Point(lane.transform, "Lane_Counter", new Vector3(-3.3f, 0f, -2.6f));
            Transform exit = Point(lane.transform, "Lane_Exit", new Vector3(0f, 0f, -10.5f));

            var slotObject = new GameObject("CustomerOrderSlot");
            slotObject.transform.SetParent(lane.transform, false);
            slotObject.transform.position = new Vector3(-3.3f, 1.1f, -2.5f);
            CarrySlot slot = slotObject.AddComponent<CarrySlot>();
            slot.AcceptAllKinds();

            GameObject spawnerObject = GameObject.Find("CustomerSpawner");
            if (spawnerObject == null)
            {
                spawnerObject = new GameObject("CustomerSpawner");
            }

            CustomerSpawner spawner = spawnerObject.GetComponent<CustomerSpawner>();
            if (spawner == null)
            {
                spawner = spawnerObject.AddComponent<CustomerSpawner>();
            }

            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("_customerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(CustomerPrefab);
            serialized.FindProperty("_laundryPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(LaundryPrefab);
            serialized.FindProperty("_orderSlot").objectReferenceValue = slot;
            serialized.FindProperty("_streetPoint").objectReferenceValue = street;
            serialized.FindProperty("_doorPoint").objectReferenceValue = door;
            serialized.FindProperty("_counterPoint").objectReferenceValue = counter;
            serialized.FindProperty("_exitPoint").objectReferenceValue = exit;

            // Customers are spawned deliberately (P5-E wiring); auto-starting here
            // would put a collider in the doorway and block the player.
            serialized.FindProperty("_autoStart").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CUSTOMER] Lane built: 4 waypoints, order slot, spawner wired.");
        }

        [MenuItem("Laundry Legends/Phase 5/Validate Customer Lane")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject lane = GameObject.Find("CustomerLane");
            GameObject spawnerObject = GameObject.Find("CustomerSpawner");
            if (lane == null || spawnerObject == null)
            {
                throw new System.InvalidOperationException("Customer lane or spawner missing.");
            }

            if (lane.transform.Find("CustomerOrderSlot")?.GetComponent<CarrySlot>() == null)
            {
                throw new System.InvalidOperationException("CustomerOrderSlot has no CarrySlot.");
            }

            CustomerSpawner spawner = spawnerObject.GetComponent<CustomerSpawner>();
            if (spawner == null)
            {
                throw new System.InvalidOperationException("CustomerSpawner component missing.");
            }

            Debug.Log("[CUSTOMER] Validation passed: lane, slot, and spawner present.");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static Transform Point(Transform parent, string name, Vector3 position)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                existing.position = position;
                return existing;
            }

            var point = new GameObject(name);
            point.transform.SetParent(parent, false);
            point.transform.position = position;
            return point.transform;
        }
    }
}
