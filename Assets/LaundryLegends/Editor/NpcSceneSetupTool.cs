using System.Collections.Generic;
using LaundryLegends.Interaction;
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
            // The staff works silently; the old laundry commentary was removed in
            // the review pass. Keep the model and the LaundryStaff behaviour.
            attendant.AddComponent<LaundryStaff>();

            GameObject regular = Instantiate("NPC_Regular", new Vector3(5.6f, 0f, -5.2f), Quaternion.Euler(0f, 180f, 0f), root.transform);
            regular.name = "PhoneRegular";
            AddPhone(regular);
            regular.AddComponent<NpcDialogue>().Configure(
                "Hello? Yes, I am at the laundry.",
                "The weather is lovely on the coast.",
                "I will pick up the kids after this.",
                "The end-of-year rush is starting.",
                "This shirt came out perfect.");
            regular.AddComponent<PickupCustomer>();

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
            voice.spatialBlend = 0f;
            voice.volume = 0.9f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 1f;
            voice.maxDistance = 14f;

            AudioClip dropoff = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/customer_hello.mp3");
            AudioClip thankyou = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/customer_thankyou.mp3");
            AmbientNpc loop = customer.AddComponent<AmbientNpc>();
            loop.Configure(waypoints, carried, voice, dropoff, thankyou);
            customer.AddComponent<NpcDialogue>().Configure(
                "I am here to drop off my laundry.",
                "The weather turned rainy this afternoon.",
                "The kids need clean uniforms for school.",
                "I will be back before the end of the year.",
                "Please check this stain if you can.");

            // No drop-off wiring on purpose. The ambient customer is scenery: the
            // staff treats anything in the order drop-off as the next order, so a
            // decorative prop placed there delays a real customer's laundry past
            // its patience. The prop stays in the NPC's hands and leaves with it.

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

        private static void AddPhone(GameObject npc)
        {
            var phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phone.name = "Phone";
            Object.DestroyImmediate(phone.GetComponent<Collider>());
            phone.transform.SetParent(npc.transform, false);
            phone.transform.localPosition = new Vector3(0.22f, 1.28f, 0.18f);
            phone.transform.localScale = new Vector3(0.06f, 0.12f, 0.02f);
            phone.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.03f, 0.03f, 0.04f, 1f)
            };
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
