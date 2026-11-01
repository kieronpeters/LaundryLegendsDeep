using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.Progression;
using LaundryLegends.UI;
using System.IO;
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
            float roomDepth = Mathf.Clamp(shop.size.z, 4f, 8f);
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
            Material carpet = EnsureCasinoCarpet();

            // Floor and ceiling.
            GameObject floorBox = CreateBox(shell.transform, "Floor", new Vector3(x0 + roomWidth * 0.5f, -0.05f, z0 + roomDepth * 0.5f),
                new Vector3(roomWidth, 0.1f, roomDepth), floor);
            if (carpet != null)
            {
                floorBox.GetComponent<Renderer>().sharedMaterial = carpet;
            }

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
            GameObject doorLeaf = GameObject.Find("SecretDoor_Foreshadowed");
            float doorZ = doorLeaf != null
                ? doorLeaf.transform.position.z
                : (shelf != null ? shelf.transform.position.z : zCentre);
            float gapMin = doorZ - doorWidth * 0.5f;
            float gapMax = doorZ + doorWidth * 0.5f;
            float southSize = Mathf.Max(0.1f, gapMin - z0);
            float northSize = Mathf.Max(0.1f, z0 + roomDepth - gapMax);

            CreateBox(shell.transform, "WallWestA", new Vector3(x0, height * 0.5f, z0 + southSize * 0.5f),
                new Vector3(0.2f, height, southSize), wall);

            CreateBox(shell.transform, "WallWestB",
                new Vector3(x0, height * 0.5f, gapMax + northSize * 0.5f),
                new Vector3(0.2f, height, northSize), wall);

            var lightObject = new GameObject("BackRoomLight");
            lightObject.transform.SetParent(shell.transform, false);
            lightObject.transform.position = new Vector3(x0 + roomWidth * 0.5f, height - 0.4f, z0 + roomDepth * 0.5f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Mathf.Max(roomWidth, roomDepth) + 2f;
            light.intensity = 6f;
            light.color = new Color(1f, 0.95f, 0.86f);

            FurnishRoom(shell.transform, x0, roomWidth, roomDepth, z0, doorZ);

            // Cabinet goes inside, against the east wall, facing the door.
            // Preserve the model's import rotation (like the street props) and
            // ground it, otherwise the pivot sinks it halfway into the floor.
            cabinet.transform.SetParent(parent, true);
            GameObject cabinetSource = PrefabUtility.GetCorrespondingObjectFromSource(cabinet.gameObject);
            Quaternion cabinetImport = cabinetSource != null
                ? cabinetSource.transform.rotation
                : Quaternion.identity;
            cabinet.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * cabinetImport;
            cabinet.transform.position = new Vector3(x0 + roomWidth - 0.9f, 0f, doorZ);
            GroundToFloor(cabinet.gameObject);

            // The model ships without colliders and the screen quad's collider is
            // removed, so the interaction ray had nothing to hit. Cover the body.
            BoxCollider cabinetCollider = cabinet.GetComponent<BoxCollider>();
            if (cabinetCollider == null)
            {
                cabinetCollider = cabinet.gameObject.AddComponent<BoxCollider>();
            }

            Bounds cabinetBounds = GetObjectBounds(cabinet.gameObject);
            cabinetCollider.center = cabinet.transform.InverseTransformPoint(cabinetBounds.center);
            Vector3 scale = cabinet.transform.lossyScale;
            cabinetCollider.size = new Vector3(
                cabinetBounds.size.x / Mathf.Max(0.0001f, scale.x),
                cabinetBounds.size.y / Mathf.Max(0.0001f, scale.y),
                cabinetBounds.size.z / Mathf.Max(0.0001f, scale.z));
            cabinetCollider.isTrigger = false;

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
                cabinet.SetService(service);
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

        /// <summary>
        /// Measures the shop interior from its floor, not from every renderer.
        /// The exterior frontage/street stretches the union bounds east and south,
        /// which used to place the back room metres away from its own door.
        /// </summary>
        private static Bounds MeasureShop()
        {
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

            if (room != null)
            {
                foreach (Transform child in room.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != "Floor")
                    {
                        continue;
                    }

                    var renderer = child.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        return renderer.bounds;
                    }
                }
            }

            return MeasureShopBounds();
        }

        /// <summary>Casino table, chairs, bar, plant, and two warm wall lights.</summary>
        private static void FurnishRoom(Transform shell, float x0, float roomWidth, float roomDepth, float z0, float doorZ)
        {
            Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/LaundryLegends/Art/Materials/MAT_CounterWood.mat");
            Material plant = AssetDatabase.LoadAssetAtPath<Material>("Assets/LaundryLegends/Art/Materials/MAT_Plant.mat");

            float tableX = x0 + roomWidth * 0.42f;
            float tableZ = Mathf.Lerp(z0 + 1.6f, doorZ - 1.6f, 0.5f);
            _ = tableX;
            _ = tableZ;

            // Bar along the south wall.
            float barZ = z0 + 0.45f;
            CreateBox(shell, "Casino_Bar", new Vector3(x0 + roomWidth * 0.62f, 0.55f, barZ),
                new Vector3(roomWidth * 0.5f, 1.1f, 0.6f), wood);
            CreateBox(shell, "Casino_BarTop", new Vector3(x0 + roomWidth * 0.62f, 1.13f, barZ),
                new Vector3(roomWidth * 0.5f + 0.12f, 0.06f, 0.74f), wood);

            // Corner plant.
            Vector3 plantBase = new Vector3(x0 + 0.7f, 0f, z0 + 0.7f);
            CreateCylinder(shell, "Casino_PlantPot", plantBase + Vector3.up * 0.22f,
                new Vector3(0.42f, 0.44f, 0.42f), wood);
            CreateSphere(shell, "Casino_PlantLeaves", plantBase + Vector3.up * 0.72f,
                new Vector3(0.7f, 0.55f, 0.7f), plant);

            // Two warm sconces flanking the door inside the room.
            Sconce(shell, new Vector3(x0 + 0.3f, 2.2f, doorZ - 1.8f));
            Sconce(shell, new Vector3(x0 + 0.3f, 2.2f, doorZ + 1.8f));

            AddPoster(
                shell,
                "Poster_StreetFolders",
                "Assets/LaundryLegends/Art/Textures/Posters/street_folders_ii.png",
                new Vector3(x0 + roomWidth * 0.5f, 1.7f, z0 + roomDepth - 0.11f),
                Quaternion.identity);
            AddPoster(
                shell,
                "Poster_SpinCycle",
                "Assets/LaundryLegends/Art/Textures/Posters/spin_cycle_showdown.png",
                new Vector3(x0 + roomWidth * 0.45f, 1.75f, z0 + 0.11f),
                Quaternion.Euler(0f, 180f, 0f));
        }

        /// <summary>Places an authored poster texture on a wall-facing quad.</summary>
        private static void AddPoster(Transform shell, string name, string texturePath, Vector3 position,
            Quaternion rotation)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[BACKROOM] Missing poster texture {texturePath}");
                return;
            }

            string materialPath = $"Assets/LaundryLegends/Art/Materials/MAT_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(lit) { name = $"MAT_{name}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = lit;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.22f, 0.22f, 0.22f));
            EditorUtility.SetDirty(material);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(shell, false);
            quad.transform.position = position;
            quad.transform.rotation = rotation;
            quad.transform.localScale = new Vector3(1.05f, 1.6f, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Sconce(Transform shell, Vector3 position)
        {
            var lightObject = new GameObject("Casino_Sconce");
            lightObject.transform.SetParent(shell, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4.5f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.78f, 0.52f);
            light.shadows = LightShadows.None;
        }

        /// <summary>
        /// Builds the black casino carpet: a gold diamond lattice with red centres
        /// on a near-black ground, tiled across the back-room floor.
        /// </summary>
        private static Material EnsureCasinoCarpet()
        {
            const string folder = "Assets/LaundryLegends/Art/Textures/Environment";
            const string texturePath = folder + "/casino_carpet.png";
            const string materialPath = "Assets/LaundryLegends/Art/Materials/MAT_CasinoCarpet.mat";

            System.IO.Directory.CreateDirectory(folder);
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = x % 32;
                    int cy = y % 32;
                    int manhattan = Mathf.Abs(cx - 16) + Mathf.Abs(cy - 16);
                    var colour = new Color32(12, 10, 16, 255);
                    if (Mathf.Abs(manhattan - 13) <= 1)
                    {
                        colour = new Color32(148, 116, 48, 255);
                    }

                    if (manhattan <= 2)
                    {
                        colour = new Color32(122, 18, 30, 255);
                    }

                    if (cx == 0 || cy == 0)
                    {
                        colour = new Color32(32, 26, 24, 255);
                    }

                    pixels[y * size + x] = colour;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            System.IO.File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(lit) { name = "MAT_CasinoCarpet" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = lit;
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.03f);
            material.SetFloat("_Metallic", 0f);
            material.mainTextureScale = new Vector2(6f, 6f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureCasinoFelt()
        {
            const string path = "Assets/LaundryLegends/Art/Materials/MAT_CasinoFelt.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(lit) { name = "MAT_CasinoFelt" };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = lit;
            }

            material.SetColor("_BaseColor", new Color(0.05f, 0.24f, 0.13f));
            material.SetFloat("_Smoothness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
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

        private static Bounds GetObjectBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(target.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void GroundToFloor(GameObject target)
        {
            Bounds bounds = GetObjectBounds(target);
            Vector3 position = target.transform.position;
            target.transform.position = new Vector3(position.x, position.y - bounds.min.y, position.z);
        }

        private static GameObject CreateCylinder(Transform parent, string name, Vector3 centre, Vector3 size,
            Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.position = centre;
            // A Unity cylinder is 2 units tall, so Y is half the requested height.
            cylinder.transform.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
            if (material != null)
            {
                cylinder.GetComponent<Renderer>().sharedMaterial = material;
            }

            return cylinder;
        }

        private static GameObject CreateSphere(Transform parent, string name, Vector3 centre, Vector3 size,
            Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.position = centre;
            sphere.transform.localScale = size;
            if (material != null)
            {
                sphere.GetComponent<Renderer>().sharedMaterial = material;
            }

            return sphere;
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

