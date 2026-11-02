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

            if (service != null)
            {
                // The shell is rebuilt below; drop references to the old slots so a
                // re-run cannot leave the service pointing at destroyed anchors.
                service.ClearExtraMachines();
            }

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

            // WallSouth is split around the expansion opening by BuildExpansionBay.

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

            GameObject expansionBay = BuildExpansionBay(
                shell.transform,
                x0,
                roomWidth,
                roomDepth,
                z0,
                height,
                floor,
                wall,
                ceiling,
                out GameObject expansionSeal);

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

                ConfigurePhase9(
                    service,
                    cabinet,
                    spawner,
                    shell.transform,
                    expansionBay,
                    expansionSeal,
                    x0,
                    roomWidth,
                    roomDepth,
                    z0,
                    doorZ,
                    height,
                    wall);
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
                traffic.SetInterval(20f);
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

        /// <summary>
        /// Builds the sealed second bay south of the room. The south wall is split
        /// around a 2.4 m opening, a removable panel seals it, and the bay root is
        /// disabled until <see cref="ArcadeService.TryExpand"/> buys it.
        /// </summary>
        private static GameObject BuildExpansionBay(
            Transform shell,
            float x0,
            float roomWidth,
            float roomDepth,
            float z0,
            float height,
            Material floor,
            Material wall,
            Material ceiling,
            out GameObject seal)
        {
            float southZ = z0 + roomDepth;
            float bayWidth = Mathf.Max(3.2f, Mathf.Min(6f, roomWidth - 1f));
            const float bayDepth = 4f;
            float cx = x0 + roomWidth * 0.5f;
            float openingWidth = Mathf.Min(2.4f, bayWidth - 1.2f);

            float sideWidth = Mathf.Max(0.1f, (roomWidth - openingWidth) * 0.5f);
            CreateBox(shell, "WallSouthA",
                new Vector3(x0 + sideWidth * 0.5f, height * 0.5f, southZ),
                new Vector3(sideWidth, height, 0.2f), wall);
            CreateBox(shell, "WallSouthB",
                new Vector3(x0 + roomWidth - sideWidth * 0.5f, height * 0.5f, southZ),
                new Vector3(sideWidth, height, 0.2f), wall);

            seal = CreateBox(shell, "ExpansionSeal",
                new Vector3(cx, height * 0.5f, southZ),
                new Vector3(openingWidth, height, 0.2f), wall);

            var bay = new GameObject("ExpansionBay");
            bay.transform.SetParent(shell, false);
            bay.SetActive(false);

            CreateBox(bay.transform, "BayFloor",
                new Vector3(cx, -0.05f, southZ + bayDepth * 0.5f),
                new Vector3(bayWidth, 0.1f, bayDepth), floor);
            CreateBox(bay.transform, "BayCeiling",
                new Vector3(cx, height, southZ + bayDepth * 0.5f),
                new Vector3(bayWidth, 0.1f, bayDepth), ceiling);
            CreateBox(bay.transform, "BayWallEast",
                new Vector3(cx + bayWidth * 0.5f, height * 0.5f, southZ + bayDepth * 0.5f),
                new Vector3(0.2f, height, bayDepth), wall);
            CreateBox(bay.transform, "BayWallWest",
                new Vector3(cx - bayWidth * 0.5f, height * 0.5f, southZ + bayDepth * 0.5f),
                new Vector3(0.2f, height, bayDepth), wall);
            CreateBox(bay.transform, "BayWallSouth",
                new Vector3(cx, height * 0.5f, southZ + bayDepth),
                new Vector3(bayWidth, height, 0.2f), wall);
            CreateBox(bay.transform, "BayThreshold",
                new Vector3(cx, 0.02f, southZ),
                new Vector3(openingWidth, 0.04f, 0.35f), floor);

            var lightObject = new GameObject("BayLight");
            lightObject.transform.SetParent(bay.transform, false);
            lightObject.transform.position = new Vector3(cx, height - 0.4f, southZ + bayDepth * 0.5f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Mathf.Max(bayWidth, bayDepth) + 2f;
            light.intensity = 5.5f;
            light.color = new Color(0.92f, 0.88f, 1f);

            return bay;
        }

        /// <summary>
        /// Adds the Phase 9 business layer: machine definitions, extra anchors,
        /// upgrade kiosks and decor, reputation, HUD and the expansion point.
        /// </summary>
        private static void ConfigurePhase9(
            ArcadeService service,
            ArcadeCabinet starter,
            CustomerSpawner spawner,
            Transform shell,
            GameObject expansionBay,
            GameObject expansionSeal,
            float x0,
            float roomWidth,
            float roomDepth,
            float z0,
            float doorZ,
            float height,
            Material wall)
        {
            const string folder = "Assets/LaundryLegends/ScriptableObjects/Arcade";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            ArcadeMachineDefinition fighter = EnsureMachineDefinition(
                folder + "/ArcadeMachine_Fighter.asset",
                "arcade.fighter", "Fighter", 120, 3, 1f);
            ArcadeMachineDefinition racer = EnsureMachineDefinition(
                folder + "/ArcadeMachine_Racer.asset",
                "arcade.racer", "Racer", 250, 5, 1.4f);
            ArcadeMachineDefinition puzzle = EnsureMachineDefinition(
                folder + "/ArcadeMachine_Puzzle.asset",
                "arcade.puzzle", "Puzzle", 400, 8, 1.8f);
            service.SetStarterDefinition(fighter);
            service.SetExpansionBay(expansionBay, expansionSeal, 300);
            service.SetUpkeep(120f, 1);

            GameObject business = GameObject.Find("ArcadeBusiness");
            if (business == null)
            {
                business = shell.gameObject;
            }

            LaundryReputation reputation = business.GetComponent<LaundryReputation>();
            if (reputation == null)
            {
                reputation = business.AddComponent<LaundryReputation>();
            }

            reputation.SetValue(20);
            service.SetReputation(reputation);
            if (spawner != null)
            {
                spawner.SetReputation(reputation);
            }

            ArcadePanelView panel = business.GetComponent<ArcadePanelView>();
            if (panel == null)
            {
                panel = business.AddComponent<ArcadePanelView>();
            }

            panel.Configure(service, reputation);

            Material wood = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/LaundryLegends/Art/Materials/MAT_CounterWood.mat");
            if (wood == null)
            {
                wood = wall;
            }

            Material felt = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/LaundryLegends/Art/Materials/MAT_CasinoFelt.mat");
            if (felt == null)
            {
                felt = wall;
            }

            Material neon = EnsureEmissiveMaterial(
                "Assets/LaundryLegends/Art/Materials/MAT_ArcadeNeon.mat",
                new Color(0.08f, 0.8f, 1f),
                3f);

            GameObject neonDecor = CreateDecorObject(
                shell, "Decor_NeonSign",
                new Vector3(x0 + roomWidth - 0.35f, 2.35f, doorZ),
                new Vector3(0.08f, 0.35f, 1.4f), neon, Quaternion.identity);

            var speakersRoot = new GameObject("Decor_Speakers");
            speakersRoot.transform.SetParent(shell, false);
            speakersRoot.SetActive(false);
            CreateDecorObject(speakersRoot.transform, "SpeakerWest",
                new Vector3(x0 + roomWidth * 0.18f, 2.1f, z0 + 0.18f),
                new Vector3(0.3f, 0.5f, 0.25f), wood, Quaternion.identity);
            CreateDecorObject(speakersRoot.transform, "SpeakerEast",
                new Vector3(x0 + roomWidth * 0.23f, 2.1f, z0 + 0.18f),
                new Vector3(0.3f, 0.5f, 0.25f), wood, Quaternion.identity);

            GameObject rugDecor = CreateDecorObject(
                shell, "Decor_ArcadeRug",
                new Vector3(x0 + roomWidth * 0.5f, 0.03f, doorZ + 1.2f),
                new Vector3(2.6f, 0.05f, 1.6f), felt, Quaternion.identity);

            float southZ = z0 + roomDepth;
            float cx = x0 + roomWidth * 0.5f;
            float bayWidth = Mathf.Max(3.2f, Mathf.Min(6f, roomWidth - 1f));

            CreateUpgradeKiosk(shell, "UpgradeKiosk_Neon",
                new Vector3(x0 + roomWidth * 0.42f, 0f, z0 + 1.4f), wood, service,
                "arcade.upgrade.neon", "Neon sign", 150, 1.1f, 1f, neonDecor,
                "Buy neon sign");
            CreateUpgradeKiosk(shell, "UpgradeKiosk_Speakers",
                new Vector3(x0 + roomWidth * 0.58f, 0f, z0 + 1.4f), wood, service,
                "arcade.upgrade.speakers", "Better speakers", 200, 1f, 1.2f, speakersRoot,
                "Buy speakers");
            CreateUpgradeKiosk(shell, "UpgradeKiosk_Carpet",
                new Vector3(x0 + roomWidth * 0.5f, 0f, z0 + 2.6f), wood, service,
                "arcade.upgrade.carpet", "Arcade carpet", 120, 1.1f, 1.1f, rugDecor,
                "Buy arcade carpet");

            GameObject expansionKiosk = CreateBox(shell, "ExpansionPoint",
                new Vector3(cx, 0.55f, southZ - 0.9f),
                new Vector3(0.8f, 1.1f, 0.5f), wood);
            var expansionPoint = expansionKiosk.AddComponent<ArcadeExpansionPoint>();
            expansionPoint.SetService(service);

            Vector3 roomCentre = new Vector3(cx, 0f, z0 + roomDepth * 0.5f);
            CreateExtraMachineSlot(
                service, shell, starter, spawner, racer, "Racer", 1,
                new Vector3(x0 + roomWidth * 0.26f, 0f, z0 + 0.95f), 180f, roomCentre, wood);
            CreateExtraMachineSlot(
                service, shell, starter, spawner, puzzle, "Puzzle", 2,
                new Vector3(x0 + roomWidth * 0.78f, 0f, southZ - 0.95f), 0f, roomCentre, wood);

            float bayZ = southZ + 2f;
            Vector3 bayRoomCentre = new Vector3(cx, 0f, southZ);
            CreateExtraMachineSlot(
                service, expansionBay.transform, starter, spawner, racer, "BayRacer", 3,
                new Vector3(cx - bayWidth * 0.22f, 0f, bayZ), 0f, bayRoomCentre, wood);
            CreateExtraMachineSlot(
                service, expansionBay.transform, starter, spawner, puzzle, "BayPuzzle", 4,
                new Vector3(cx + bayWidth * 0.22f, 0f, bayZ), 0f, bayRoomCentre, wood);
        }

        private static ArcadeMachineDefinition EnsureMachineDefinition(
            string path,
            string id,
            string displayName,
            int cost,
            int baseIncome,
            float attractWeight)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ArcadeMachineDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ArcadeMachineDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(id, displayName, cost, baseIncome, attractWeight);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static Material EnsureEmissiveMaterial(string path, Color color, float intensity)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(lit) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = lit;
            }

            material.SetColor("_BaseColor", color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            material.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateDecorObject(
            Transform parent,
            string name,
            Vector3 centre,
            Vector3 size,
            Material material,
            Quaternion rotation)
        {
            GameObject decor = CreateBox(parent, name, centre, size, material);
            decor.transform.rotation = rotation;
            decor.SetActive(false);
            return decor;
        }

        private static void CreateUpgradeKiosk(
            Transform parent,
            string name,
            Vector3 floorPosition,
            Material material,
            ArcadeService service,
            string id,
            string displayName,
            int cost,
            float incomeMultiplier,
            float trafficMultiplier,
            GameObject decor,
            string prompt)
        {
            GameObject kiosk = CreateBox(
                parent,
                name,
                floorPosition + Vector3.up * 0.55f,
                new Vector3(0.55f, 1.1f, 0.45f),
                material);
            var point = kiosk.AddComponent<ArcadeUpgradePoint>();
            point.ConfigureInline(
                service, id, displayName, cost, incomeMultiplier, trafficMultiplier, decor, prompt);
        }

        /// <summary>
        /// Duplicates the shipped cabinet for one extra slot, places it at its
        /// anchor and leaves it inactive until the slot is bought.
        /// </summary>
        private static void CreateExtraMachineSlot(
            ArcadeService service,
            Transform parent,
            ArcadeCabinet starter,
            CustomerSpawner spawner,
            ArcadeMachineDefinition definition,
            string label,
            int expectedSlot,
            Vector3 position,
            float yaw,
            Vector3 roomCentre,
            Material kioskMaterial)
        {
            int slotIndex = service.SlotCount;
            if (slotIndex != expectedSlot)
            {
                Debug.LogWarning($"[BACKROOM] Slot order drift: {label} expected {expectedSlot}, got {slotIndex}.");
            }

            var anchor = new GameObject($"Anchor_{label}");
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = position;
            anchor.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Vector3 towardRoom = roomCentre - position;
            towardRoom.y = 0f;
            if (towardRoom.sqrMagnitude < 0.01f)
            {
                towardRoom = Vector3.forward;
            }

            towardRoom.Normalize();
            GameObject install = CreateBox(
                parent,
                $"Install_{label}",
                position + towardRoom * 1.15f + Vector3.up * 0.55f,
                new Vector3(0.6f, 1.1f, 0.45f),
                kioskMaterial);
            install.transform.rotation = Quaternion.LookRotation(towardRoom, Vector3.up);
            install.AddComponent<ArcadeInstallPoint>();

            GameObject cabinetObject = Object.Instantiate(starter.gameObject);
            cabinetObject.name = $"ArcadeCabinet_{label}";
            cabinetObject.transform.SetParent(parent, true);

            // The starter is already rotated 90 degrees from its imported pose.
            Quaternion importRotation = starter.transform.rotation * Quaternion.Euler(0f, -90f, 0f);
            cabinetObject.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * importRotation;
            cabinetObject.transform.position = position;
            GroundToFloor(cabinetObject);

            ArcadeCabinet cabinet = cabinetObject.GetComponent<ArcadeCabinet>();
            if (cabinet == null)
            {
                cabinet = cabinetObject.AddComponent<ArcadeCabinet>();
            }

            Renderer screen = ResolveScreen(cabinet);
            FightGameView view = cabinetObject.GetComponent<FightGameView>();
            if (view == null)
            {
                view = cabinetObject.AddComponent<FightGameView>();
            }

            view.Configure(screen);
            cabinet.Configure(view, spawner);
            cabinet.SetService(service, slotIndex);

            ArcadeAttractMode attract = cabinetObject.GetComponent<ArcadeAttractMode>();
            if (attract == null)
            {
                attract = cabinetObject.AddComponent<ArcadeAttractMode>();
            }

            AudioSource audio = cabinetObject.GetComponent<AudioSource>();
            if (audio == null)
            {
                audio = cabinetObject.AddComponent<AudioSource>();
            }

            attract.Configure(screen, audio);
            cabinetObject.SetActive(false);

            service.AddSlot(anchor.transform, install, definition, cabinetObject, attract);
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

