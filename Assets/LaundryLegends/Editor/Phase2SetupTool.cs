using System;
using System.Collections.Generic;
using System.IO;
using LaundryLegends.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Adds the Phase 2 physical-environment pass to the existing greybox room:
    /// a storefront entrance, south-wall windows, and a minimal outside backdrop.
    /// The tool is additive and idempotent; it never rebuilds unrelated room content.
    /// </summary>
    public static class Phase2SetupTool
    {
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string LaundromatScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        private static readonly string[] PublicZoneRootNames =
        {
            "WaitingArea",
            "StorageWest",
            "UtilityAlcove",
            "Signage"
        };

        private static readonly string[] GeneratedChildNames =
        {
            "Wall_South_Left_PierA",
            "Wall_South_Left_Sill",
            "Wall_South_Left_Header",
            "Wall_South_Left_PierB",
            "Wall_South_Right_PierA",
            "Wall_South_Right_Sill",
            "Wall_South_Right_Header",
            "Wall_South_Right_PierB",
            "Entrance_Jamb_Left",
            "Entrance_Jamb_Right",
            "Entrance_Header",
            "Entrance_Threshold",
            "Entrance_Door_Left",
            "Entrance_Door_Right",
            "Window_Left_FrameLeft",
            "Window_Left_FrameRight",
            "Window_Left_FrameTop",
            "Window_Left_FrameBottom",
            "Window_Left_Mullion",
            "Window_Left_Glass",
            "Window_Right_FrameLeft",
            "Window_Right_FrameRight",
            "Window_Right_FrameTop",
            "Window_Right_FrameBottom",
            "Window_Right_Mullion",
            "Window_Right_Glass",
            "Facade_Backdrop",
            "Facade_Ground"
        };

        [MenuItem("Laundry Legends/Phase 2/Build Entrance Envelope")]
        public static void BuildEntranceEnvelope()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath) == null)
            {
                throw new InvalidOperationException($"Missing room prefab: {RoomPrefabPath}");
            }

            Material wall = RequireMaterial("Grey_Wall");
            Material metal = RequireMaterial("Grey_Metal");
            Material floor = RequireMaterial("Grey_Floor");
            Material glass = GetOrCreateGlassMaterial();
            Material sky = GetOrCreateSkyMaterial();

            GameObject contents = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform root = contents.transform;
                RemoveGeneratedChildren(root);
                RemoveChild(root, "Wall_South_Left");
                RemoveChild(root, "Wall_South_Right");

                BuildSouthWalls(root, wall);
                BuildEntrance(root, metal, glass);
                BuildWindow(root, -5f, "Window_Left", metal, glass);
                BuildWindow(root, 5f, "Window_Right", metal, glass);
                BuildExterior(root, floor, sky);

                PrefabUtility.SaveAsPrefabAsset(contents, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            ConfigureSceneExterior();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2] Entrance envelope built into the greybox room prefab.");
        }

        [MenuItem("Laundry Legends/Phase 2/Validate Entrance Envelope")]
        public static void ValidateEntranceEnvelope()
        {
            var failures = new List<string>();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing room prefab: {RoomPrefabPath}");
            }
            else
            {
                ValidatePrefabStructure(prefab, failures);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                GameObject room = FindRoot(scene, "GreyboxRoom");
                if (room == null)
                {
                    failures.Add("Laundromat has no GreyboxRoom instance.");
                }
                else
                {
                    Physics.SyncTransforms();
                    ValidateSceneInstance(room, failures);
                    ValidateEntranceRoute(failures);
                    ValidateSecretDoorStillClosed(room, failures);
                }

                FirstPersonController player = FindPlayer(scene);
                if (player == null)
                {
                    failures.Add("Laundromat has no player.");
                }
                else if (player.transform.position.y < -0.1f)
                {
                    failures.Add($"Player spawn is below the floor: y={player.transform.position.y:0.00}.");
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Phase2] {failure}");
                }

                throw new InvalidOperationException($"Entrance envelope validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Phase2] Validation passed: entrance, windows, exterior backdrop, collision, and existing room content are present.");
        }

        [MenuItem("Laundry Legends/Phase 2/Capture Entrance Envelope")]
        public static void CaptureEntranceEnvelope()
        {
            EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            // View from inside the room looking back toward the storefront.
            CaptureView("phase2-entrance.png", new Vector3(0f, 1.7f, -1.2f), new Vector3(0f, 1.25f, -6f));
            // View across the room toward the south windows.
            CaptureView("phase2-storefront.png", new Vector3(0f, 1.7f, 3.2f), new Vector3(-5f, 1.6f, -6f));

            Debug.Log("[Phase2] Captured entrance/storefront previews under TestResults/.");
        }

        [MenuItem("Laundry Legends/Phase 2/Build Waiting, Storage, Utility, Signage")]
        public static void BuildPublicZones()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath) == null)
            {
                throw new InvalidOperationException($"Missing room prefab: {RoomPrefabPath}");
            }

            Material wall = RequireMaterial("Grey_Wall");
            Material metal = RequireMaterial("Grey_Metal");
            Material counter = RequireMaterial("Grey_Counter");
            Material washer = RequireMaterial("Grey_Washer");
            Material dryer = RequireMaterial("Grey_Dryer");
            Material chair = GetOrCreateColoredMaterial("Grey_Chair", new Color(0.22f, 0.40f, 0.42f), 0.2f, 0f);
            Material sign = GetOrCreateColoredMaterial("Grey_Sign", new Color(0.07f, 0.08f, 0.09f), 0.35f, 0f);

            GameObject contents = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform root = contents.transform;
                RemovePublicZones(root);

                BuildWaitingArea(root, chair, metal, counter);
                BuildStorage(root, metal, counter, washer, dryer);
                BuildUtilityAlcove(root, wall, metal, counter, sign);
                BuildSignage(root, sign);

                PrefabUtility.SaveAsPrefabAsset(contents, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            ConfigureSceneExterior();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2] Waiting area, storage, utility alcove, and signage built into the greybox room prefab.");
        }

        [MenuItem("Laundry Legends/Phase 2/Validate Public Zones")]
        public static void ValidatePublicZones()
        {
            var failures = new List<string>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing room prefab: {RoomPrefabPath}");
            }
            else
            {
                ValidatePublicZoneStructure(prefab, failures);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                GameObject room = FindRoot(scene, "GreyboxRoom");
                if (room == null)
                {
                    failures.Add("Laundromat has no GreyboxRoom instance.");
                }
                else
                {
                    Physics.SyncTransforms();
                    ValidatePublicZoneScene(room, failures);
                    ValidateSecretDoorStillClosed(room, failures);
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Phase2] {failure}");
                }

                throw new InvalidOperationException($"Public-zone validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Phase2] Public-zone validation passed: waiting area, storage, utility alcove, signage, clearance, and secret-door closure are valid.");
        }

        [MenuItem("Laundry Legends/Phase 2/Capture Public Zones")]
        public static void CapturePublicZones()
        {
            EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            // Waiting chairs and the south-east storefront windows.
            CaptureView("phase2-waiting.png", new Vector3(1.2f, 1.7f, -2.6f), new Vector3(5.4f, 0.7f, -4.9f));
            // Machine-wall sign and the central working aisle.
            CaptureView("phase2-machine-signage.png", new Vector3(0f, 1.7f, 1.2f), new Vector3(0f, 1.9f, 5.6f));
            // North-west utility alcove and its staff-only divider.
            CaptureView("phase2-utility.png", new Vector3(-2.6f, 1.7f, 3.1f), new Vector3(-7.0f, 0.9f, 4.7f));

            Debug.Log("[Phase2] Captured waiting, machine-signage, and utility previews under TestResults/.");
        }

        [MenuItem("Laundry Legends/Phase 2/Build Secret Door Concealment")]
        public static void BuildSecretConcealment()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath) == null)
            {
                throw new InvalidOperationException($"Missing room prefab: {RoomPrefabPath}");
            }

            Material metal = RequireMaterial("Grey_Metal");
            Material counter = RequireMaterial("Grey_Counter");
            Material sign = RequireMaterial("Grey_Sign");

            GameObject contents = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform root = contents.transform;
                RemoveChild(root, "SecretAccess");
                BuildSecretAccess(root, metal, counter, sign);
                PrefabUtility.SaveAsPrefabAsset(contents, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2] Secret-door concealment built into the greybox room prefab.");
        }

        [MenuItem("Laundry Legends/Phase 2/Validate Secret Door Concealment")]
        public static void ValidateSecretConcealment()
        {
            var failures = new List<string>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (prefab == null)
            {
                failures.Add($"Missing room prefab: {RoomPrefabPath}");
            }
            else
            {
                ValidateSecretAccessStructure(prefab.transform, failures);
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                GameObject room = FindRoot(scene, "GreyboxRoom");
                if (room == null)
                {
                    failures.Add("Laundromat has no GreyboxRoom instance.");
                }
                else
                {
                    Physics.SyncTransforms();
                    ValidateSecretAccessScene(room, failures);
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Phase2] {failure}");
                }

                throw new InvalidOperationException($"Secret-door concealment validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Phase2] Secret-door concealment validation passed: cover, foreshadowing marker, collider containment, and closed door are valid.");
        }

        [MenuItem("Laundry Legends/Phase 2/Capture Secret Door Approach")]
        public static void CaptureSecretApproach()
        {
            EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            // Approach from the public aisle: the cover should read as ordinary service storage.
            CaptureView("phase2-secret-approach.png", new Vector3(3.0f, 1.7f, 3.0f), new Vector3(7.8f, 1.1f, 3.0f));
            // Close three-quarter view: visible frame edge, casters, and service marker foreshadow the access.
            CaptureView("phase2-secret-close.png", new Vector3(4.8f, 1.75f, 1.1f), new Vector3(7.8f, 1.3f, 3.0f));

            Debug.Log("[Phase2] Captured secret-door approach and close previews under TestResults/.");
        }

        [MenuItem("Laundry Legends/Phase 2/Validate Public Navigation")]
        public static void ValidatePublicNavigation()
        {
            var failures = new List<string>();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LaundromatScenePath) == null)
            {
                failures.Add($"Missing scene: {LaundromatScenePath}");
            }
            else
            {
                Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);
                GameObject room = FindRoot(scene, "GreyboxRoom");
                if (room == null)
                {
                    failures.Add("Laundromat has no GreyboxRoom instance.");
                }
                else
                {
                    Physics.SyncTransforms();
                    ValidateNavigationRoutes(failures);
                    ValidateCriticalColliders(room, failures);
                    ValidateCameraClearance(failures);
                }
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Phase2] {failure}");
                }

                throw new InvalidOperationException($"Public-navigation validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Phase2] Public-navigation validation passed: routes, critical colliders, materials, and camera clearance are valid.");
        }

        private static void ValidateNavigationRoutes(List<string> failures)
        {
            (string Label, Vector3 Start, Vector3 End)[] routes =
            {
                ("entrance-to-counter", new Vector3(0f, 1.1f, -4.1f), new Vector3(-2.7f, 1.1f, -2.4f)),
                ("central aisle", new Vector3(0f, 1.1f, -3.8f), new Vector3(0f, 1.1f, 3.8f)),
                ("machine aisle", new Vector3(0f, 1.1f, 3.8f), new Vector3(5.8f, 1.1f, 3.8f)),
                ("east circulation", new Vector3(6.4f, 1.1f, 3.8f), new Vector3(6.7f, 1.1f, -3.6f)),
                ("waiting-to-central", new Vector3(6.7f, 1.1f, -3.6f), new Vector3(0f, 1.1f, -3.6f)),
                ("central-to-entrance", new Vector3(0f, 1.1f, -3.6f), new Vector3(0f, 1.1f, -4.1f)),
                ("counter-to-entrance", new Vector3(-2.7f, 1.1f, -2.4f), new Vector3(0f, 1.1f, -4.1f))
            };

            foreach (var route in routes)
            {
                if (Physics.Linecast(route.Start, route.End, out RaycastHit hit))
                {
                    failures.Add($"{route.Label} is blocked by {hit.collider.gameObject.name} at {hit.distance:0.00} m.");
                }
            }
        }

        private static void ValidateCriticalColliders(GameObject room, List<string> failures)
        {
            string[] critical =
            {
                "Counter",
                "FoldingTable",
                "Machine_1",
                "WaitingArea/Waiting_Chair_1",
                "WaitingArea/Waiting_Chair_2",
                "WaitingArea/Waiting_SideTable",
                "StorageWest/Storage_Shelf_1",
                "StorageWest/Storage_Shelf_2",
                "UtilityAlcove/Utility_Divider",
                "UtilityAlcove/Utility_Cabinet",
                "SecretAccess/SecretAccess_ServicePanel",
                "SecretAccess/SecretAccess_Shelf"
            };

            foreach (string path in critical)
            {
                Transform target = room.transform.Find(path);
                if (target == null)
                {
                    failures.Add($"Critical object is missing: {path}.");
                }
                else if (target.GetComponentInChildren<Collider>(true) == null)
                {
                    failures.Add($"Critical object has no collider: {path}.");
                }
            }

            foreach (Renderer renderer in room.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == null)
                {
                    failures.Add($"Renderer has no material: {renderer.gameObject.name}.");
                }
            }

            foreach (MonoBehaviour behaviour in room.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    failures.Add("Room contains a missing MonoBehaviour reference.");
                }
            }
        }

        private static void ValidateCameraClearance(List<string> failures)
        {
            Vector3[] samples =
            {
                new Vector3(0f, 0f, -3.8f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 3.6f),
                new Vector3(-2.6f, 0f, -2.3f),
                new Vector3(2f, 0f, -3.6f),
                new Vector3(5.8f, 0f, 3.6f),
                new Vector3(6.4f, 0f, -3.6f),
                new Vector3(4.5f, 0f, 3.6f)
            };

            float[] eyeHeights = { 1.5f, 1.7f };
            foreach (Vector3 sample in samples)
            {
                foreach (float eyeHeight in eyeHeights)
                {
                    Vector3 eye = new Vector3(sample.x, eyeHeight, sample.z);
                    if (Physics.CheckSphere(eye, 0.2f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        failures.Add($"Camera clearance is blocked at ({sample.x:0.0}, {eyeHeight:0.0}, {sample.z:0.0}).");
                    }
                }
            }
        }

        private static void BuildSecretAccess(Transform root, Material metal, Material counter, Material sign)
        {
            Transform group = CreateGroup(root, "SecretAccess");

            // A shallow service panel bridges the door frame; the supply shelf in front hides
            // its center while leaving the outer frame and handle side readable from close up.
            CreateBox("SecretAccess_ServicePanel", group, new Vector3(7.70f, 1.04f, 3f), new Vector3(0.05f, 1.92f, 1.35f), metal);

            Transform shelf = CreateGroup(group, "SecretAccess_Shelf");
            shelf.localPosition = new Vector3(7.52f, 0f, 3f);
            CreateBox("SecretAccess_Shelf_SideA", shelf, new Vector3(0f, 1f, -0.62f), new Vector3(0.34f, 1.8f, 0.06f), metal);
            CreateBox("SecretAccess_Shelf_SideB", shelf, new Vector3(0f, 1f, 0.62f), new Vector3(0.34f, 1.8f, 0.06f), metal);

            float[] shelfHeights = { 0.55f, 1f, 1.45f, 1.85f };
            for (int i = 0; i < shelfHeights.Length; i++)
            {
                CreateBox($"SecretAccess_Shelf_Board_{i + 1}", shelf, new Vector3(0f, shelfHeights[i], 0f), new Vector3(0.32f, 0.04f, 1.22f), counter);
            }

            CreateCylinder("SecretAccess_Caster_FL", shelf, new Vector3(-0.10f, 0.05f, 0.52f), new Vector3(0.10f, 0.05f, 0.10f), metal);
            CreateCylinder("SecretAccess_Caster_FR", shelf, new Vector3(0.10f, 0.05f, 0.52f), new Vector3(0.10f, 0.05f, 0.10f), metal);
            CreateCylinder("SecretAccess_Caster_BL", shelf, new Vector3(-0.10f, 0.05f, -0.52f), new Vector3(0.10f, 0.05f, 0.10f), metal);
            CreateCylinder("SecretAccess_Caster_BR", shelf, new Vector3(0.10f, 0.05f, -0.52f), new Vector3(0.10f, 0.05f, 0.10f), metal);

            CreateBox("SecretAccess_SupplyBin", shelf, new Vector3(0f, 0.75f, 0.18f), new Vector3(0.26f, 0.26f, 0.34f), sign);

            // A small, mundane service marker above the cover is the deliberate foreshadowing clue.
            CreateSign(group, "SecretAccess_Marker", new Vector3(7.87f, 2.47f, 3f), new Vector3(0.9f, 0.18f, 0.04f), sign,
                "SERVICE", 0.06f, 90f);
        }

        private static void ValidateSecretAccessStructure(Transform root, List<string> failures)
        {
            RequireChild(root, "SecretAccess", failures);
            RequireChild(root, "SecretAccess/SecretAccess_ServicePanel", failures);
            RequireChild(root, "SecretAccess/SecretAccess_Shelf", failures);
            RequireChild(root, "SecretAccess/SecretAccess_Marker", failures);
            RequireCollider(root, "SecretAccess/SecretAccess_ServicePanel", failures);
            RequireCollider(root, "SecretAccess/SecretAccess_Shelf", failures);

            Transform door = root.Find("SecretDoor_Foreshadowed");
            Transform shelf = root.Find("SecretAccess/SecretAccess_Shelf");
            if (door == null || shelf == null)
            {
                return;
            }

            Bounds doorBounds = GetBounds(door.gameObject);
            Bounds shelfBounds = GetBounds(shelf.gameObject);
            if (shelfBounds.min.x >= doorBounds.min.x)
            {
                failures.Add("Secret-access cover must sit in front of the door on the room side.");
            }

            if (shelfBounds.max.y >= doorBounds.max.y - 0.05f)
            {
                failures.Add("Secret-access cover should leave the top of the door frame visible.");
            }

            if (shelfBounds.min.z > doorBounds.min.z + 0.05f || shelfBounds.max.z < doorBounds.max.z - 0.05f)
            {
                failures.Add("Secret-access cover should span the door width.");
            }

            RequireSignText(root, "SecretAccess/SecretAccess_Marker", "SERVICE", failures);
        }

        private static void ValidateSecretAccessScene(GameObject room, List<string> failures)
        {
            Transform door = room.transform.Find("SecretDoor_Foreshadowed");
            if (door == null)
            {
                failures.Add("SecretDoor_Foreshadowed is missing from the Laundromat scene.");
                return;
            }

            Collider doorCollider = door.GetComponent<Collider>();
            if (doorCollider == null || !doorCollider.enabled)
            {
                failures.Add("SecretDoor_Foreshadowed collider must remain enabled.");
            }

            Transform cover = room.transform.Find("SecretAccess");
            Transform shelf = room.transform.Find("SecretAccess/SecretAccess_Shelf");
            if (cover == null || shelf == null)
            {
                failures.Add("SecretAccess cover is missing from the Laundromat scene.");
                return;
            }

            Bounds doorBounds = GetBounds(door.gameObject);
            Bounds shelfBounds = GetBounds(shelf.gameObject);
            if (shelfBounds.min.x >= doorBounds.min.x)
            {
                failures.Add("Secret-access cover must sit in front of the door on the room side.");
            }

            if (shelfBounds.max.y >= doorBounds.max.y - 0.05f)
            {
                failures.Add("Secret-access cover should leave the top of the door frame visible.");
            }

            if (shelfBounds.min.z > doorBounds.min.z + 0.05f || shelfBounds.max.z < doorBounds.max.z - 0.05f)
            {
                failures.Add("Secret-access cover should span the door width.");
            }

            // The first thing between the public aisle and the door must be the cover, not the door.
            if (!Physics.Raycast(new Vector3(6f, 1.2f, 3f), Vector3.right, out RaycastHit hit, 3f))
            {
                failures.Add("No collider blocks the path from the public aisle to the secret door.");
            }
            else if (!hit.collider.transform.IsChildOf(cover))
            {
                failures.Add($"The secret door is exposed to the aisle; first hit is {hit.collider.gameObject.name}.");
            }

            // Side rays must still terminate on the frame or wall rather than an open passage.
            CheckRay(failures, "secret-access left edge", new Vector3(6f, 1.2f, 2.3f), Vector3.right, 3f);
            CheckRay(failures, "secret-access right edge", new Vector3(6f, 1.2f, 3.7f), Vector3.right, 3f);
        }

        private static void CheckRay(List<string> failures, string label, Vector3 origin, Vector3 direction, float distance)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance))
            {
                failures.Add($"{label}: expected a collider within {distance:0.0} m.");
            }
        }

        private static void BuildSouthWalls(Transform root, Material wall)
        {
            // Left segment: x -8..-2, window opening at x -6.2..-3.8, y 0.9..2.4.
            CreateBox("Wall_South_Left_PierA", root, new Vector3(-7.1f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);
            CreateBox("Wall_South_Left_Sill", root, new Vector3(-5f, 0.45f, -6f), new Vector3(2.4f, 0.9f, 0.2f), wall);
            CreateBox("Wall_South_Left_Header", root, new Vector3(-5f, 2.8f, -6f), new Vector3(2.4f, 0.8f, 0.2f), wall);
            CreateBox("Wall_South_Left_PierB", root, new Vector3(-2.9f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);

            // Right segment: x 2..8, window opening at x 3.8..6.2, y 0.9..2.4.
            CreateBox("Wall_South_Right_PierA", root, new Vector3(2.9f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);
            CreateBox("Wall_South_Right_Sill", root, new Vector3(5f, 0.45f, -6f), new Vector3(2.4f, 0.9f, 0.2f), wall);
            CreateBox("Wall_South_Right_Header", root, new Vector3(5f, 2.8f, -6f), new Vector3(2.4f, 0.8f, 0.2f), wall);
            CreateBox("Wall_South_Right_PierB", root, new Vector3(7.1f, 1.6f, -6f), new Vector3(1.8f, 3.2f, 0.2f), wall);
        }

        private static void BuildEntrance(Transform root, Material metal, Material glass)
        {
            CreateBox("Entrance_Jamb_Left", root, new Vector3(-2.1f, 1.2f, -5.95f), new Vector3(0.2f, 2.4f, 0.25f), metal);
            CreateBox("Entrance_Jamb_Right", root, new Vector3(2.1f, 1.2f, -5.95f), new Vector3(0.2f, 2.4f, 0.25f), metal);
            CreateBox("Entrance_Header", root, new Vector3(0f, 2.5f, -5.95f), new Vector3(4.4f, 0.2f, 0.25f), metal);
            CreateBox("Entrance_Threshold", root, new Vector3(0f, 0.025f, -5.95f), new Vector3(4.2f, 0.05f, 0.35f), metal);

            // Open glass leaves rest against the interior side of each jamb.
            CreateBox("Entrance_Door_Left", root, new Vector3(-2.05f, 1.15f, -4.95f), new Vector3(0.06f, 2.3f, 1.9f), glass);
            CreateBox("Entrance_Door_Right", root, new Vector3(2.05f, 1.15f, -4.95f), new Vector3(0.06f, 2.3f, 1.9f), glass);
        }

        private static void BuildWindow(Transform root, float centerX, string prefix, Material metal, Material glass)
        {
            CreateBox(prefix + "_FrameLeft", root, new Vector3(centerX - 1.24f, 1.65f, -5.94f), new Vector3(0.08f, 1.6f, 0.12f), metal);
            CreateBox(prefix + "_FrameRight", root, new Vector3(centerX + 1.24f, 1.65f, -5.94f), new Vector3(0.08f, 1.6f, 0.12f), metal);
            CreateBox(prefix + "_FrameTop", root, new Vector3(centerX, 2.42f, -5.94f), new Vector3(2.56f, 0.08f, 0.12f), metal);
            CreateBox(prefix + "_FrameBottom", root, new Vector3(centerX, 0.88f, -5.94f), new Vector3(2.56f, 0.08f, 0.12f), metal);
            CreateBox(prefix + "_Mullion", root, new Vector3(centerX, 1.65f, -5.94f), new Vector3(0.06f, 1.5f, 0.1f), metal);
            CreateBox(prefix + "_Glass", root, new Vector3(centerX, 1.65f, -5.98f), new Vector3(2.44f, 1.5f, 0.04f), glass);
        }

        private static void BuildExterior(Transform root, Material floor, Material sky)
        {
            CreateBox("Facade_Ground", root, new Vector3(0f, -0.3f, -7.35f), new Vector3(20f, 0.6f, 2.5f), floor);
            CreateBox("Facade_Backdrop", root, new Vector3(0f, 2.2f, -8.7f), new Vector3(20f, 5f, 0.2f), sky);
        }

        private static void BuildWaitingArea(Transform root, Material chair, Material metal, Material counter)
        {
            Transform group = CreateGroup(root, "WaitingArea");
            CreateChair(group, "Waiting_Chair_1", new Vector3(4.4f, 0f, -4.9f), chair, metal);
            CreateChair(group, "Waiting_Chair_2", new Vector3(5.3f, 0f, -4.9f), chair, metal);

            var table = new GameObject("Waiting_SideTable");
            table.transform.SetParent(group, false);
            table.transform.localPosition = new Vector3(6.2f, 0f, -4.9f);
            CreateBox("Waiting_SideTable_Top", table.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.06f, 0.5f), counter);
            CreateBox("Waiting_SideTable_Pedestal", table.transform, new Vector3(0f, 0.27f, 0f), new Vector3(0.12f, 0.52f, 0.12f), metal);
            CreateBox("Waiting_SideTable_Base", table.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.34f, 0.04f, 0.34f), metal);

            CreateBox("Waiting_Planter", group, new Vector3(6.9f, 0.18f, -5.3f), new Vector3(0.34f, 0.36f, 0.34f), metal);
            CreateBox("Waiting_Planter_Foliage", group, new Vector3(6.9f, 0.48f, -5.3f), new Vector3(0.42f, 0.3f, 0.42f), chair);
        }

        private static void CreateChair(Transform parent, string name, Vector3 localPosition, Material chair, Material metal)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.localPosition = localPosition;
            Transform chairRoot = group.transform;

            CreateBox(name + "_Seat", chairRoot, new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.12f, 0.55f), chair);
            CreateBox(name + "_Back", chairRoot, new Vector3(0f, 0.78f, -0.22f), new Vector3(0.55f, 0.6f, 0.1f), chair);

            const float legX = 0.22f;
            const float legZ = 0.22f;
            CreateBox(name + "_Leg_FL", chairRoot, new Vector3(-legX, 0.22f, legZ), new Vector3(0.05f, 0.44f, 0.05f), metal);
            CreateBox(name + "_Leg_FR", chairRoot, new Vector3(legX, 0.22f, legZ), new Vector3(0.05f, 0.44f, 0.05f), metal);
            CreateBox(name + "_Leg_BL", chairRoot, new Vector3(-legX, 0.22f, -legZ), new Vector3(0.05f, 0.44f, 0.05f), metal);
            CreateBox(name + "_Leg_BR", chairRoot, new Vector3(legX, 0.22f, -legZ), new Vector3(0.05f, 0.44f, 0.05f), metal);
        }

        private static void BuildStorage(Transform root, Material metal, Material counter, Material washer, Material dryer)
        {
            Transform group = CreateGroup(root, "StorageWest");
            CreateShelfUnit(group, "Storage_Shelf_1", new Vector3(-7.6f, 0f, -0.5f), metal, counter, washer, dryer);
            CreateShelfUnit(group, "Storage_Shelf_2", new Vector3(-7.6f, 0f, 1.1f), metal, counter, washer, dryer);
        }

        private static void CreateShelfUnit(
            Transform parent,
            string name,
            Vector3 localPosition,
            Material metal,
            Material counter,
            Material washer,
            Material dryer)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.localPosition = localPosition;
            Transform shelf = group.transform;

            CreateBox(name + "_Back", shelf, new Vector3(-0.18f, 1.0f, 0f), new Vector3(0.04f, 2.0f, 1.2f), metal);
            CreateBox(name + "_SideA", shelf, new Vector3(0f, 1.0f, -0.58f), new Vector3(0.36f, 2.0f, 0.04f), metal);
            CreateBox(name + "_SideB", shelf, new Vector3(0f, 1.0f, 0.58f), new Vector3(0.36f, 2.0f, 0.04f), metal);

            float[] shelfHeights = { 0.35f, 0.85f, 1.35f, 1.85f };
            for (int i = 0; i < shelfHeights.Length; i++)
            {
                CreateBox($"{name}_Shelf_{i + 1}", shelf, new Vector3(0f, shelfHeights[i], 0f), new Vector3(0.34f, 0.05f, 1.12f), counter);
            }

            CreateBox(name + "_Bin_A", shelf, new Vector3(0f, 0.52f, -0.28f), new Vector3(0.28f, 0.28f, 0.36f), washer);
            CreateBox(name + "_Bin_B", shelf, new Vector3(0f, 1.02f, 0.24f), new Vector3(0.28f, 0.28f, 0.36f), dryer);
            CreateBox(name + "_Bin_C", shelf, new Vector3(0f, 1.52f, -0.24f), new Vector3(0.28f, 0.28f, 0.36f), counter);
        }

        private static void BuildUtilityAlcove(Transform root, Material wall, Material metal, Material counter, Material sign)
        {
            Transform group = CreateGroup(root, "UtilityAlcove");
            CreateBox("Utility_Divider", group, new Vector3(-5.05f, 0.525f, 4.4f), new Vector3(0.12f, 1.05f, 1.8f), wall);
            CreateBox("Utility_DividerCap", group, new Vector3(-5.05f, 1.08f, 4.4f), new Vector3(0.18f, 0.06f, 1.9f), metal);

            CreateBox("Utility_Cabinet", group, new Vector3(-7.2f, 0.45f, 4.95f), new Vector3(0.7f, 0.9f, 0.55f), metal);
            CreateBox("Utility_CabinetTop", group, new Vector3(-7.2f, 0.93f, 4.95f), new Vector3(0.78f, 0.06f, 0.62f), counter);
            CreateBox("Utility_Sink", group, new Vector3(-7.2f, 0.98f, 4.95f), new Vector3(0.5f, 0.08f, 0.34f), sign);
            CreateCylinder("Utility_MopBucket", group, new Vector3(-6.55f, 0.25f, 4.55f), new Vector3(0.28f, 0.25f, 0.28f), metal);

            CreateSign(group, "Utility_Sign", new Vector3(-4.96f, 0.78f, 4.4f), new Vector3(1.0f, 0.22f, 0.04f), sign,
                "STAFF ONLY", 0.075f, -90f);
        }

        private static void BuildSignage(Transform root, Material sign)
        {
            Transform group = CreateGroup(root, "Signage");
            CreateSign(group, "Sign_Storefront", new Vector3(0f, 2.86f, -6.12f), new Vector3(4.0f, 0.45f, 0.08f), sign,
                "LAUNDRY LEGENDS", 0.14f, 0f);
            CreateSign(group, "Sign_MachineWall", new Vector3(0f, 2.55f, 5.62f), new Vector3(6.0f, 0.5f, 0.06f), sign,
                "WASH   DRY   FOLD", 0.14f, 0f);
            CreateSign(group, "Sign_Counter", new Vector3(-4.5f, 1.36f, -2.98f), new Vector3(1.2f, 0.24f, 0.04f), sign,
                "DROP OFF", 0.08f, 0f);
            CreateSign(group, "Sign_Open", new Vector3(4.5f, 1.62f, -5.90f), new Vector3(0.6f, 0.26f, 0.04f), sign,
                "OPEN", 0.075f, 180f);
        }

        private static void CreateSign(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 panelScale,
            Material panelMaterial,
            string text,
            float characterSize,
            float yRotation)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.localPosition = localPosition;
            group.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            CreateBox(name + "_Panel", group.transform, Vector3.zero, panelScale, panelMaterial);

            var textObject = new GameObject(name + "_Text");
            textObject.transform.SetParent(group.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -(panelScale.z * 0.5f + 0.012f));
            TextMesh mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = LoadLegacyFont();
            mesh.fontSize = 64;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.95f, 0.95f, 0.9f);
            textObject.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
        }

        private static Font LoadLegacyFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font == null)
            {
                throw new InvalidOperationException("No built-in legacy font is available for placeholder signage.");
            }

            return font;
        }

        private static Transform CreateGroup(Transform root, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(root, false);
            return group.transform;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localScale = localScale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            return cylinder;
        }

        private static void ConfigureSceneExterior()
        {
            Scene scene = EditorSceneManager.OpenScene(LaundromatScenePath, OpenSceneMode.Single);

            // Keep the existing ambient settings; the new facade adds no scene-level lights.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.25f, 0.28f);
            RenderSettings.fog = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidatePrefabStructure(GameObject prefab, List<string> failures)
        {
            RequireChild(prefab, "Floor", failures);
            RequireChild(prefab, "Ceiling", failures);
            RequireChild(prefab, "MachineBackPanel", failures);
            RequireChild(prefab, "SecretDoor_Foreshadowed", failures);
            RequireChild(prefab, "Entrance_Jamb_Left", failures);
            RequireChild(prefab, "Entrance_Jamb_Right", failures);
            RequireChild(prefab, "Entrance_Header", failures);
            RequireChild(prefab, "Entrance_Threshold", failures);
            RequireChild(prefab, "Window_Left_Glass", failures);
            RequireChild(prefab, "Window_Right_Glass", failures);
            RequireChild(prefab, "Facade_Backdrop", failures);
            RequireChild(prefab, "Facade_Ground", failures);

            if (prefab.transform.Find("Wall_South_Left") != null || prefab.transform.Find("Wall_South_Right") != null)
            {
                failures.Add("Original solid south wall segments were not replaced.");
            }

            if (prefab.GetComponentsInChildren<Collider>(true).Length < 30)
            {
                failures.Add("Prefab should have at least 30 colliders after the facade pass.");
            }

            Transform threshold = prefab.transform.Find("Entrance_Threshold");
            if (threshold != null && threshold.localScale.y > 0.1f)
            {
                failures.Add($"Entrance threshold is too tall: {threshold.localScale.y:0.00} m.");
            }
        }

        private static void ValidateSceneInstance(GameObject room, List<string> failures)
        {
            RequireChild(room, "Entrance_Threshold", failures);
            RequireChild(room, "Window_Left_Glass", failures);
            RequireChild(room, "Window_Right_Glass", failures);
            RequireChild(room, "Facade_Backdrop", failures);
            RequireChild(room, "Facade_Ground", failures);
        }

        private static void ValidateEntranceRoute(List<string> failures)
        {
            // The center of the doorway must stay open all the way to the exterior.
            // Start inside the opening, past the player spawn capsule.
            if (Physics.Raycast(new Vector3(0f, 1.2f, -5.0f), Vector3.back, out RaycastHit hit, 1.85f))
            {
                failures.Add($"Doorway center is blocked by {hit.collider.gameObject.name} at {hit.distance:0.00} m.");
            }

            CheckWindowRay(failures, -5f, "Window_Left_Glass");
            CheckWindowRay(failures, 5f, "Window_Right_Glass");
        }

        private static void CheckWindowRay(List<string> failures, float centerX, string expectedName)
        {
            // Offset from the center mullion so the ray tests the glass pane.
            float sampleX = centerX - 0.8f;
            if (!Physics.Raycast(new Vector3(sampleX, 1.65f, -4.5f), Vector3.back, out RaycastHit hit, 1.6f))
            {
                failures.Add($"No window collision found at x={sampleX:0.0}.");
                return;
            }

            if (hit.collider.gameObject.name != expectedName)
            {
                failures.Add($"Window ray at x={sampleX:0.0} hit {hit.collider.gameObject.name}, expected {expectedName}.");
            }
        }

        private static void ValidateSecretDoorStillClosed(GameObject room, List<string> failures)
        {
            Transform door = room.transform.Find("SecretDoor_Foreshadowed");
            if (door == null)
            {
                failures.Add("SecretDoor_Foreshadowed is missing after the facade pass.");
                return;
            }

            Collider collider = door.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
            {
                failures.Add("SecretDoor_Foreshadowed collider must remain enabled after the facade pass.");
            }
        }

        private static void RequireChild(GameObject parent, string name, List<string> failures)
        {
            RequireChild(parent.transform, name, failures);
        }

        private static void RequireChild(Transform parent, string name, List<string> failures)
        {
            if (parent.Find(name) == null)
            {
                failures.Add($"Missing expected object: {name}.");
            }
        }

        private static void ValidatePublicZoneStructure(GameObject prefab, List<string> failures)
        {
            Transform root = prefab.transform;
            RequireChild(root, "WaitingArea", failures);
            RequireChild(root, "WaitingArea/Waiting_Chair_1", failures);
            RequireChild(root, "WaitingArea/Waiting_Chair_2", failures);
            RequireChild(root, "WaitingArea/Waiting_SideTable", failures);
            RequireChild(root, "StorageWest/Storage_Shelf_1", failures);
            RequireChild(root, "StorageWest/Storage_Shelf_2", failures);
            RequireChild(root, "UtilityAlcove/Utility_Divider", failures);
            RequireChild(root, "UtilityAlcove/Utility_Cabinet", failures);
            RequireChild(root, "UtilityAlcove/Utility_Sign", failures);
            Transform legacyStorefront = root.Find("Signage/Sign_Storefront");
            if (legacyStorefront != null)
            {
                RequireChild(root, "Signage/Sign_Storefront", failures);
            }
            else
            {
                RequireChild(root, "FrontageRoot/Facade_SignPanel", failures);
            }

            RequireChild(root, "Signage/Sign_MachineWall", failures);
            RequireChild(root, "Signage/Sign_Counter", failures);
            RequireChild(root, "Signage/Sign_Open", failures);

            RequireCollider(root, "WaitingArea/Waiting_Chair_1", failures);
            RequireCollider(root, "WaitingArea/Waiting_Chair_2", failures);
            RequireCollider(root, "WaitingArea/Waiting_SideTable", failures);
            RequireCollider(root, "StorageWest/Storage_Shelf_1", failures);
            RequireCollider(root, "StorageWest/Storage_Shelf_2", failures);
            RequireCollider(root, "UtilityAlcove/Utility_Divider", failures);
            RequireCollider(root, "UtilityAlcove/Utility_Cabinet", failures);

            if (root.Find("Signage/Sign_Storefront") != null)
            {
                RequireSignText(root, "Signage/Sign_Storefront", "LAUNDRY LEGENDS", failures);
            }
            else
            {
                RequireSignText(root, "FrontageRoot/Facade_SignText_Outside", "LAUNDRY LEGENDS", failures);
            }

            RequireSignText(root, "Signage/Sign_MachineWall", "WASH", failures);
            if (root.Find("Signage/Sign_Counter/Sign_Counter_Text") != null)
            {
                RequireSignText(root, "Signage/Sign_Counter", "DROP OFF", failures);
            }
            else
            {
                RequireSignText(root, "FrontageRoot/DropOff_Text", "DROP OFF", failures);
            }

            if (root.Find("Signage/Sign_Open/Sign_Open_Text") != null)
            {
                RequireSignText(root, "Signage/Sign_Open", "OPEN", failures);
            }
            else
            {
                RequireSignText(root, "FrontageRoot/Facade_HoursText_Outside", "OPEN", failures);
            }

            RequireSignText(root, "UtilityAlcove/Utility_Sign", "STAFF ONLY", failures);

            CheckBounds(root, "WaitingArea", bounds => bounds.min.x > 3.8f,
                "Waiting area must stay east of the central entrance-to-counter lane.", failures);
            CheckBounds(root, "StorageWest", bounds => bounds.max.x < -7.3f,
                "Storage shelving must stay against the west wall and leave the counter aisle clear.", failures);
            CheckBounds(root, "UtilityAlcove", bounds => bounds.max.x < -4.8f,
                "Utility alcove must stay in the north-west corner and off the machine aisle.", failures);

            Transform storefront = root.Find("Signage/Sign_Storefront/Sign_Storefront_Panel");
            if (storefront == null)
            {
                storefront = root.Find("FrontageRoot/Facade_SignPanel");
            }

            if (storefront != null && GetBounds(storefront.gameObject).min.y < 2.1f)
            {
                failures.Add("Storefront sign hangs below the 2.1 m clearance line.");
            }

            Transform machineSign = root.Find("Signage/Sign_MachineWall/Sign_MachineWall_Panel");
            if (machineSign != null && GetBounds(machineSign.gameObject).min.y < 2.1f)
            {
                failures.Add("Machine-wall sign hangs below the 2.1 m clearance line.");
            }
        }

        private static void ValidatePublicZoneScene(GameObject room, List<string> failures)
        {
            RequireChild(room.transform, "WaitingArea", failures);
            RequireChild(room.transform, "StorageWest", failures);
            RequireChild(room.transform, "UtilityAlcove", failures);
            RequireChild(room.transform, "Signage", failures);

            // Start past the player capsule at the spawn point so the validation target is not
            // reported as an obstruction.
            if (Physics.Linecast(new Vector3(0f, 1.1f, -4.1f), new Vector3(0f, 1.1f, 4.3f), out RaycastHit centralHit))
            {
                failures.Add($"Central aisle is blocked by {centralHit.collider.gameObject.name} at {centralHit.distance:0.00} m.");
            }

            if (Physics.Linecast(new Vector3(0f, 1.1f, -4.1f), new Vector3(-2.7f, 1.1f, -2.4f), out RaycastHit counterHit))
            {
                failures.Add($"Entrance-to-counter route is blocked by {counterHit.collider.gameObject.name} at {counterHit.distance:0.00} m.");
            }
        }

        private static void RequireCollider(Transform root, string path, List<string> failures)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                return;
            }

            if (target.GetComponentInChildren<Collider>(true) == null)
            {
                failures.Add($"Expected a collider under {path}.");
            }
        }

        private static void RequireSignText(Transform root, string path, string expected, List<string> failures)
        {
            Transform sign = root.Find(path);
            if (sign == null)
            {
                return;
            }

            TextMesh text = sign.GetComponentInChildren<TextMesh>(true);
            if (text == null)
            {
                failures.Add($"Sign {path} has no TextMesh.");
            }
            else if (!text.text.Contains(expected))
            {
                failures.Add($"Sign {path} text '{text.text}' does not contain '{expected}'.");
            }
        }

        private static void CheckBounds(Transform root, string path, Func<Bounds, bool> predicate, string message, List<string> failures)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                return;
            }

            if (!predicate(GetBounds(target.gameObject)))
            {
                failures.Add(message);
            }
        }

        private static Bounds GetBounds(GameObject target)
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

        private static void RemovePublicZones(Transform root)
        {
            foreach (string name in PublicZoneRootNames)
            {
                RemoveChild(root, name);
            }
        }

        private static void RemoveGeneratedChildren(Transform root)
        {
            foreach (string name in GeneratedChildNames)
            {
                RemoveChild(root, name);
            }
        }

        private static void RemoveChild(Transform root, string name)
        {
            Transform child = root.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static Material RequireMaterial(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
            if (material == null)
            {
                throw new InvalidOperationException($"Missing greybox material: {name}.");
            }

            return material;
        }

        private static Material GetOrCreateGlassMaterial()
        {
            string path = $"{MaterialFolder}/Grey_Glass.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Standard shader is unavailable.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.color = new Color(0.55f, 0.75f, 0.85f, 0.45f);
            material.SetFloat("_Glossiness", 0.85f);
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateSkyMaterial()
        {
            string path = $"{MaterialFolder}/Grey_Sky.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Standard shader is unavailable.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = new Color(0.58f, 0.72f, 0.84f);
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.18f, 0.25f, 0.33f));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateColoredMaterial(string name, Color color, float glossiness, float metallic)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Standard shader is unavailable.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Glossiness", glossiness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CaptureView(string fileName, Vector3 position, Vector3 lookAt)
        {
            var cameraObject = new GameObject("Phase2PreviewCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(lookAt);

            var renderTexture = new RenderTexture(1280, 720, 24);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();

            string directory = Path.GetFullPath("TestResults");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static FirstPersonController FindPlayer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FirstPersonController player = root.GetComponentInChildren<FirstPersonController>(true);
                if (player != null)
                {
                    return player;
                }
            }

            return null;
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
