using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.Progression;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the back room behind the concealed shelf: a shell roughly half the
    /// laundromat's size, the arcade cabinet inside it, the $200 purchase point in
    /// front of the shelf, and the map component. Idempotent: re-running reuses the
    /// objects it created.
    /// </summary>
    public static class HiddenRoomSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string FloorMaterial = "Assets/LaundryLegends/Art/Materials/MAT_FloorTile.mat";
        private const string WallMaterial = "Assets/LaundryLegends/Art/Materials/MAT_WallPaint.mat";
        private const string CeilingMaterial = "Assets/LaundryLegends/Art/Materials/MAT_CeilingFlat.mat";

        [MenuItem("Laundry Legends/Phase 8/Build Back Room")]
        public static void Build()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var room = Object.FindFirstObjectByType<SecretRoomController>();
            var cabinet = Object.FindFirstObjectByType<ArcadeCabinet>(FindObjectsInactive.Include);
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            var service = Object.FindFirstObjectByType<ArcadeService>(FindObjectsInactive.Include);

            if (room == null || cabinet == null)
            {
                Debug.LogError($"[BACKROOM] room={room != null} cabinet={cabinet != null}");
                return;
            }

            var serializedRoom = new SerializedObject(room);
            GameObject content = serializedRoom.FindProperty("_roomContent")?.objectReferenceValue as GameObject;
            GameObject shelf = serializedRoom.FindProperty("_sealedDoorCover")?.objectReferenceValue as GameObject;

            Bounds shop = MeasureShop();
            string keepName = "BackRoomShell";
            foreach (Transform child in (content != null ? content.transform : null)?.GetComponentsInChildren<Transform>(true) ?? new Transform[0])
            {
                if (child.name == keepName)
                {
                    Object.DestroyImmediate(child.gameObject);
                    break;
                }
            }

            // Half the shop's floor area, through the east wall.
            float roomWidth = Mathf.Max(4f, shop.size.x * 0.5f);
            float roomDepth = Mathf.Max(3f, shop.size.z * 0.6f);
            float x0 = shop.max.x + 0.2f;
            float zCentre = shop.center.z;
            float z0 = zCentre - roomDepth * 0.5f;
            float height = 2.9f;

            Transform parent = content != null ? content.transform : null;
            var shell = new GameObject(keepName);
            if (parent != null)
            {
                shell.transform.SetParent(parent, false);
            }

            Material floor = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterial);
            Material wall = AssetDatabase.LoadAssetAtPath<Material>(WallMaterial);
            Material ceiling = AssetDatabase.LoadAssetAtPath<Material>(CeilingMaterial);

            // Floor and ceiling.
            CreateBox(shell.transform, "Floor", new Vector3(x0 + roomWidth * 0.5f, -0.05f, z0 + roomDepth * 0.5f),
                new Vector3(roomWidth, 0.1f, roomDepth), floor);

            CreateBox(shell.transform, "Ceiling", new Vector3(x0 + roomWidth * 0.5f, height, z0 + roomDepth * 0.5f),
                new Vector3(roomWidth, 0.1f, roomDepth), ceiling);

            // Walls: back (east), north, south, and the west wall split around the door gap.
            CreateBox(shell.transform, "WallEast", new Vector3(x0 + roomWidth, height * 0.5f, z0 + roomDepth * 0.5f),
                new Vector3(0.2f, height, roomDepth), wall);

            CreateBox(shell.transform, "WallNorth", new Vector3(x0 + roomWidth * 0.5f, height * 0.5f, z0),
                new Vector3(roomWidth, height, 0.2f), wall);

            CreateBox(shell.transform, "WallSouth", new Vector3(x0 + roomWidth * 0.5f, height * 0.5f, z0 + roomDepth),
                new Vector3(roomWidth, height, 0.2f), wall);

            const float doorWidth = 1.6f;
            float doorZ = shelf != null ? shelf.transform.position.z : zCentre;
            float westSide = roomDepth * 0.5f - doorWidth * 0.5f;

            CreateBox(shell.transform, "WallWestA", new Vector3(x0, height * 0.5f, z0 + westSide * 0.5f),
                new Vector3(0.2f, height, westSide), wall);

            CreateBox(shell.transform, "WallWestB",
                new Vector3(x0, height * 0.5f, z0 + roomDepth - westSide * 0.5f),
                new Vector3(0.2f, height, westSide), wall);

            var lightObject = new GameObject("BackRoomLight");
            lightObject.transform.SetParent(shell.transform, false);
            lightObject.transform.position = new Vector3(x0 + roomWidth * 0.5f, height - 0.4f, z0 + roomDepth * 0.5f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Mathf.Max(roomWidth, roomDepth) + 2f;
            light.intensity = 6f;
            light.color = new Color(1f, 0.95f, 0.86f);

            // Cabinet goes inside, against the east wall, facing the door.
            cabinet.transform.SetParent(parent, true);
            cabinet.transform.position = new Vector3(x0 + roomWidth - 0.9f, 0f, doorZ);
            cabinet.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // The machine ships installed, so the purchase point is retired.
            ArcadeInstallPoint installPoint = Object.FindFirstObjectByType<ArcadeInstallPoint>();
            if (installPoint != null)
            {
                Object.DestroyImmediate(installPoint.gameObject);
            }

            // $200 shelf purchase, in front of the concealed door.
            Transform shelfTransform = shelf != null ? shelf.transform : cabinet.transform;
            GameObject purchaseObject = shelf;
            if (purchaseObject == null)
            {
                purchaseObject = new GameObject("BackRoomShelf");
                purchaseObject.transform.position = new Vector3(shop.max.x - 0.6f, 1f, doorZ);
            }

            HiddenRoomPurchasePoint purchase = purchaseObject.GetComponent<HiddenRoomPurchasePoint>();
            if (purchase == null)
            {
                purchase = purchaseObject.AddComponent<HiddenRoomPurchasePoint>();
            }

            purchase.SetRoom(room);
            purchase.SetSpawner(spawner);

            Collider shelfCollider = purchaseObject.GetComponent<Collider>();
            if (shelfCollider == null)
            {
                purchaseObject.AddComponent<BoxCollider>();
            }

            // Map, opened with M.
            GameObject business = GameObject.Find("ArcadeBusiness");
            if (business != null)
            {
                ShopMapView map = business.GetComponent<ShopMapView>();
                if (map == null)
                {
                    map = business.AddComponent<ShopMapView>();
                }

                map.Configure(room);
            }

            // The cabinet may still carry the old maze view; make sure the fight view
            // exists and is pointed at the cabinet screen.
            Renderer cabinetScreen = ResolveScreen(cabinet);
            FightGameView fightView = cabinet.GetComponent<FightGameView>();
            if (fightView == null)
            {
                fightView = cabinet.gameObject.AddComponent<FightGameView>();
            }

            fightView.Configure(cabinetScreen);
            cabinet.Configure(fightView, spawner);

            if (service != null)
            {
                service.SetStartActive(true);
            }

            // Visitors must approach the machine from inside the room, not through its wall.
            ArcadeTraffic traffic = Object.FindFirstObjectByType<ArcadeTraffic>(FindObjectsInactive.Include);
            if (traffic != null && service != null)
            {
                Transform lane = null;
                foreach (Transform child in shell.transform)
                {
                    if (child.name == "VisitorLane")
                    {
                        lane = child;
                        break;
                    }
                }

                if (lane == null)
                {
                    var laneObject = new GameObject("VisitorLane");
                    laneObject.transform.SetParent(shell.transform, false);
                    laneObject.transform.position = new Vector3(x0 + 1.1f, 0f, doorZ);
                    lane = laneObject.transform;
                }

                var visitorPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Customer.prefab");
                traffic.Configure(service, lane, cabinet.transform, visitorPrefab);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BACKROOM] room {roomWidth:0.0} x {roomDepth:0.0} m at x={x0:0.0}, cabinet moved to {cabinet.transform.position}, shop {shop.size}");
        }

        private static Bounds MeasureShop()
        {
            return MeasureShopBounds();
        }

        /// <summary>Finds the cabinet's screen renderer from the old view or by name.</summary>
        private static Renderer ResolveScreen(ArcadeCabinet cabinet)
        {
            MazeGameView maze = cabinet.GetComponentInChildren<MazeGameView>(true);
            if (maze != null)
            {
                var serialized = new SerializedObject(maze);
                SerializedProperty screen = serialized.FindProperty("_screen");
                if (screen != null && screen.objectReferenceValue is Renderer fromMaze)
                {
                    return fromMaze;
                }
            }

            foreach (Renderer renderer in cabinet.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == "Screen")
                {
                    return renderer;
                }
            }

            return cabinet.GetComponentInChildren<Renderer>(true);
        }

        private static Bounds MeasureShopBounds()        {
            Transform room = null;
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "GreyboxRoom")
                    {
                        room = child;
                        break;
                    }
                }

                if (room != null)
                {
                    break;
                }
            }

            if (room == null)
            {
                return new Bounds(Vector3.zero, new Vector3(16f, 3f, 10f));
            }

            var bounds = new Bounds(room.position, Vector3.zero);
            bool first = true;
            foreach (Renderer renderer in room.GetComponentsInChildren<Renderer>(true))
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 centre, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = centre;
            box.transform.localScale = size;

            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.AddComponent<BoxCollider>();

            if (material != null)
            {
                box.GetComponent<Renderer>().sharedMaterial = material;
            }

            return box;
        }
    }
}

