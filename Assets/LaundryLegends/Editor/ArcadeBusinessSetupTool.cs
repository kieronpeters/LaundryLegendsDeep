using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.Player;
using LaundryLegends.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Wires the arcade business loop into the Laundromat scene: purchase point,
    /// service, attract mode and visitor traffic. Safe to re-run; existing objects
    /// are reused instead of duplicated.
    /// </summary>
    public static class ArcadeBusinessSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string PlayerPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/Player.prefab";
        private const string VisitorPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Customer.prefab";

        [MenuItem("Laundry Legends/Phase 8/Wire Arcade Business")]
        public static void Wire()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ArcadeCabinet cabinet = FindComponent<ArcadeCabinet>("ArcadePlaceholder");
            SecretRoomController room = FindComponent<SecretRoomController>("SecretRoomSystem");
            CustomerSpawner spawner = FindComponent<CustomerSpawner>("CustomerSpawner");

            if (cabinet == null)
            {
                Debug.LogError("[ARCADE] ArcadePlaceholder/ArcadeCabinet not found; aborting.");
                return;
            }

            GameObject business = EnsureChild(null, "ArcadeBusiness");
            ArcadeService service = EnsureComponent<ArcadeService>(business);
            ArcadeTraffic traffic = EnsureComponent<ArcadeTraffic>(business);

            GameObject attractObject = EnsureChild(business.transform, "AttractMode");
            ArcadeAttractMode attract = EnsureComponent<ArcadeAttractMode>(attractObject);
            AudioSource audio = EnsureComponent<AudioSource>(attractObject);
            audio.playOnAwake = false;
            audio.spatialBlend = 0.6f;
            audio.volume = 0.35f;

            Renderer screen = ResolveCabinetScreen(cabinet);
            attract.Configure(screen, audio);

            FightGameView fightView = EnsureComponent<FightGameView>(cabinet.gameObject);
            fightView.Configure(screen);
            cabinet.Configure(fightView, spawner);

            // A small panel beside the cabinet is the purchase point. It needs its own
            // collider so the player's interaction ray can actually hit it.
            GameObject installObject = EnsureChild(cabinet.transform, "InstallPoint");
            installObject.transform.localPosition = new Vector3(-1.1f, 1.05f, 0.35f);
            installObject.transform.localRotation = Quaternion.identity;
            if (installObject.GetComponent<Collider>() == null)
            {
                BoxCollider box = installObject.AddComponent<BoxCollider>();
                box.size = new Vector3(0.35f, 0.5f, 0.12f);
            }

            if (installObject.GetComponentInChildren<Renderer>(true) == null)
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "InstallPointVisual";
                visual.transform.SetParent(installObject.transform, false);
                visual.transform.localScale = new Vector3(0.35f, 0.5f, 0.12f);
                Collider stray = visual.GetComponent<Collider>();
                if (stray != null)
                {
                    Object.DestroyImmediate(stray);
                }
            }

            ArcadeInstallPoint installPoint = EnsureComponent<ArcadeInstallPoint>(installObject);
            installPoint.SetService(service);

            GameObject visitorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisitorPrefab);
            Transform spawn = FindTransform("CustomerLane");
            if (spawn == null)
            {
                spawn = business.transform;
            }

            service.Configure(room, cabinet, attract, cabinet.transform, spawner);
            // Reachable after a couple of orders rather than seven: 100 start + 40/order.
            service.SetMachineCost(120);
            traffic.Configure(service, spawn, cabinet.transform, visitorPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            PatchPlayerHands();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ARCADE] Wired: room={(room != null)} spawner={(spawner != null)} screen={(screen != null)} visitor={(visitorPrefab != null)}");
        }

        private static Renderer ResolveCabinetScreen(ArcadeCabinet cabinet)
        {
            MazeGameView view = cabinet.GetComponentInChildren<MazeGameView>(true);
            if (view == null)
            {
                return null;
            }

            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty screen = serialized.FindProperty("_screen");
            return screen != null ? screen.objectReferenceValue as Renderer : null;
        }

        /// <summary>
        /// Adds the carry-driven hand visibility to the player prefab and points it at
        /// the existing hand meshes.
        /// </summary>
        private static void PatchPlayerHands()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                Transform hands = null;
                CarryController carry = contents.GetComponent<CarryController>();

                foreach (Transform child in contents.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "FirstPersonHands")
                    {
                        hands = child;
                        break;
                    }
                }

                if (hands == null || carry == null)
                {
                    Debug.LogWarning($"[ARCADE] hands={hands != null} carry={carry != null}; player not patched.");
                    return;
                }

                FirstPersonHandsView view = contents.GetComponent<FirstPersonHandsView>();
                if (view == null)
                {
                    view = contents.AddComponent<FirstPersonHandsView>();
                }

                view.Configure(hands.gameObject, carry);
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefab);
                Debug.Log("[ARCADE] Player hands now show only while carrying.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static T FindComponent<T>(string objectName) where T : Component
        {
            foreach (T candidate in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == objectName || candidate.gameObject.transform.root.name == objectName)
                {
                    return candidate;
                }
            }

            return Object.FindFirstObjectByType<T>();
        }

        private static Transform FindTransform(string objectName)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            if (parent == null)
            {
                foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (root.name == name)
                    {
                        return root;
                    }
                }

                var created = new GameObject(name);
                return created;
            }

            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}

