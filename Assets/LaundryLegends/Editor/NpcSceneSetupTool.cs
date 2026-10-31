using System.Collections.Generic;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Places ambient NPCs in the Laundromat: a static attendant, a waiting
    /// regular, and a customer who walks in from the street, waits, and leaves.
    /// </summary>
    public static class NpcSceneSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string PrefabFolder = "Assets/LaundryLegends/Prefabs/Gameplay/NPC";
        private const string LaundryPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab";
        private const string VoiceFolder = "Assets/LaundryLegends/Audio/Voice";
        private const string RootName = "AmbientActors";

        [MenuItem("Laundry Legends/Phase 3/Build Ambient NPCs")]
        public static void BuildAmbientNpcs()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject existing = FindRoot(scene, RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            GameObject attendant = Instantiate("NPC_Attendant", new Vector3(-4.6f, 0f, -1.9f), Quaternion.Euler(0f, 180f, 0f), root.transform);
            attendant.name = "Attendant";

            GameObject customer = Instantiate("NPC_Customer", new Vector3(0f, 0f, -10.5f), Quaternion.identity, root.transform);
            customer.name = "StreetCustomer";

            Transform[] waypoints =
            {
                CreateWaypoint(root.transform, "Waypoint_Street", new Vector3(0f, 0f, -10.5f)),
                CreateWaypoint(root.transform, "Waypoint_Door", new Vector3(0f, 0f, -5.6f)),
                CreateWaypoint(root.transform, "Waypoint_Counter", new Vector3(-3.9f, 0f, -2.0f)),
                CreateWaypoint(root.transform, "Waypoint_DoorOut", new Vector3(0f, 0f, -5.6f)),
                CreateWaypoint(root.transform, "Waypoint_StreetOut", new Vector3(0f, 0f, -10.5f))
            };

            GameObject carried = AssetDatabase.LoadAssetAtPath<GameObject>(LaundryPrefab) != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LaundryPrefab), customer.transform)
                : null;
            if (carried != null)
            {
                carried.name = "CarriedLaundry";
                carried.transform.localPosition = new Vector3(0.35f, 1.0f, 0.25f);
                carried.transform.localRotation = Quaternion.identity;
                foreach (Collider collider in carried.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }

                foreach (Rigidbody body in carried.GetComponentsInChildren<Rigidbody>(true))
                {
                    body.isKinematic = true;
                    body.useGravity = false;
                }
            }

            AudioSource voice = customer.AddComponent<AudioSource>();
            voice.spatialBlend = 1f;
            voice.volume = 0.8f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 1f;
            voice.maxDistance = 14f;

            AudioClip dropoff = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/dropoff.wav");
            AudioClip thankyou = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/thankyou.wav");
            AmbientNpc loop = customer.AddComponent<AmbientNpc>();
            loop.Configure(waypoints, carried, voice, dropoff, thankyou);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NPC] Ambient attendant and street customer placed.");
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Ambient NPCs")]
        public static void ValidateAmbientNpcs()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindRoot(scene, RootName);
            if (root == null)
            {
                throw new System.InvalidOperationException("Laundromat has no AmbientActors root.");
            }

            AmbientNpc customer = root.GetComponentInChildren<AmbientNpc>(true);
            if (customer == null)
            {
                throw new System.InvalidOperationException("No ambient customer found.");
            }

            if (customer.WaypointCount < 4)
            {
                throw new System.InvalidOperationException("Ambient customer needs at least four waypoints.");
            }

            if (customer.Voice == null)
            {
                throw new System.InvalidOperationException("Ambient customer has no voice source.");
            }

            if (customer.CarriedProp == null)
            {
                throw new System.InvalidOperationException("Ambient customer has no carried laundry.");
            }

            if (Object.FindObjectsByType<AmbientNpc>(FindObjectsSortMode.None).Length != 1)
            {
                throw new System.InvalidOperationException("Expected exactly one scripted ambient customer.");
            }

            Debug.Log("[NPC] Ambient NPC validation passed: attendant, street customer, route, carried laundry, and voice source.");
        }

        private static GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Missing NPC prefab: {prefabName}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            return instance;
        }

        private static Transform CreateWaypoint(Transform parent, string name, Vector3 position)
        {
            var waypoint = new GameObject(name);
            waypoint.transform.SetParent(parent, false);
            waypoint.transform.position = position;
            return waypoint.transform;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
