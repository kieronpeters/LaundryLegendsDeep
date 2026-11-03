using System.Collections.Generic;
using LaundryLegends.Customers;
using LaundryLegends.Interaction;
using LaundryLegends.NPC;
using LaundryLegends.Progression;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Repairs and validates the Phase 8 playtest wiring in the Laundromat scene.
    /// <para>
    /// The back-room purchase point was attached to the thin service panel, but the
    /// rolling shelf stands in front of it. The player's interaction ray hit a shelf
    /// board with no interactable on it, so the $200 purchase never offered a
    /// prompt. Moving the component to the <c>SecretAccess</c> group makes every
    /// part of the concealment resolve to it. The same group is the controller's
    /// "cover", so buying clears the whole thing and opens the passage.
    /// </para>
    /// <para>
    /// The decorative ambient customer also dropped its laundry prop into the
    /// order drop-off. The staff picks up whatever is there, so one decorative
    /// prop starved the real order pipeline for a whole wash/dry cycle, which is
    /// the "one order, then nothing" playtest symptom. The scene now leaves that
    /// link unassigned.
    /// </para>
    /// </summary>
    public static class PlaytestFixTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Repair Playtest Wiring")]
        public static void Repair()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var report = new List<string>();

            RepairShelfPurchase(report);
            RepairHud(report);
            RepairAmbientDropOff(report);
            RepairCustomerCounterPoint(report);
            RepairStaffStandPoint(report);
            RepairSecretDoorPivot(report);
            RepairPassageGap(report);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FIX] " + string.Join("\n[FIX] ", report));
        }

        private static void RepairShelfPurchase(List<string> report)
        {
            GameObject cover = GameObject.Find("SecretAccess");
            var room = Object.FindFirstObjectByType<SecretRoomController>();
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            if (cover == null || room == null || spawner == null)
            {
                report.Add($"shelf repair skipped: cover={cover != null} room={room != null} spawner={spawner != null}");
                return;
            }

            HiddenRoomPurchasePoint purchase = cover.GetComponent<HiddenRoomPurchasePoint>();
            if (purchase == null)
            {
                purchase = cover.AddComponent<HiddenRoomPurchasePoint>();
            }

            purchase.SetRoom(room);
            purchase.SetSpawner(spawner);

            // Remove the old panel-hosted component so there is exactly one.
            foreach (HiddenRoomPurchasePoint other in
                     Object.FindObjectsByType<HiddenRoomPurchasePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != purchase)
                {
                    report.Add($"removed a second purchase point from '{other.gameObject.name}'");
                    Object.DestroyImmediate(other);
                }
            }

            // The cover is the whole concealment, so paying clears the passage
            // instead of leaving the shelf blocking the doorway.
            var serializedRoom = new SerializedObject(room);
            SerializedProperty doorCover = serializedRoom.FindProperty("_sealedDoorCover");
            string previous = doorCover.objectReferenceValue != null
                ? doorCover.objectReferenceValue.name
                : "NONE";
            doorCover.objectReferenceValue = cover;
            serializedRoom.ApplyModifiedPropertiesWithoutUndo();

            report.Add($"purchase point on '{cover.name}', door cover {previous} -> {cover.name}");
        }

        private static void RepairHud(List<string> report)
        {
            OrderTicketView[] tickets = Object.FindObjectsByType<OrderTicketView>(FindObjectsSortMode.None);
            foreach (OrderTicketView ticket in tickets)
            {
                CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
                if (ticket.Spawner == null && spawner != null)
                {
                    ticket.Configure(spawner);
                }

                var serialized = new SerializedObject(ticket);
                serialized.FindProperty("_width").floatValue = 300f;
                serialized.FindProperty("_height").floatValue = 290f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            report.Add($"order ticket views repaired: {tickets.Length}");
        }

        /// <summary>
        /// The customer's counter waypoint sat inside the counter's box collider.
        /// Because NPC overlap correction runs after movement, the body could
        /// never enter that spot: the customer stalled ~0.3 m short, timed out,
        /// and left without a ticket. Walk the point out of any furniture along
        /// the room side until the NPC capsule is clear.
        /// </summary>
        private static void RepairCustomerCounterPoint(List<string> report)
        {
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            if (spawner == null)
            {
                report.Add("counter point repair skipped: no CustomerSpawner");
                return;
            }

            var serialized = new SerializedObject(spawner);
            var counterPoint = serialized.FindProperty("_counterPoint").objectReferenceValue as Transform;
            if (counterPoint == null)
            {
                report.Add("counter point repair skipped: _counterPoint is not assigned");
                return;
            }

            Vector3 original = counterPoint.position;
            Vector3 point = original;
            for (int step = 0; step < 40 && IsCustomerBodyBlocked(point); step++)
            {
                point += new Vector3(0f, 0f, 0.05f);
            }

            if ((point - original).sqrMagnitude > 0.000001f)
            {
                counterPoint.position = point;
                report.Add(
                    $"customer counter point moved {original.ToString("0.00")} -> {point.ToString("0.00")} " +
                    "(it sat inside the counter collider)");
            }
            else
            {
                report.Add("customer counter point was already clear");
            }
        }

        private static bool IsCustomerBodyBlocked(Vector3 point)
        {
            Vector3 bottom = point + Vector3.up * 0.35f;
            Vector3 top = point + Vector3.up * 1.3f;
            return Physics.OverlapCapsuleNonAlloc(
                       bottom, top, 0.3f, s_bodyProbe, ~0, QueryTriggerInteraction.Ignore) > 0;
        }

        private static readonly Collider[] s_bodyProbe = new Collider[16];

        /// <summary>
        /// The drop-off slot sits on the counter, so the staff's old 0.55 m stand
        /// point fell inside the counter collider. Overlap correction then pinned
        /// the staff at the counter face and the order never left the counter.
        /// </summary>
        private static void RepairStaffStandPoint(List<string> report)
        {
            LaundryStaff staff = Object.FindFirstObjectByType<LaundryStaff>(FindObjectsInactive.Include);
            if (staff == null)
            {
                report.Add("staff stand point skipped: no LaundryStaff");
                return;
            }

            var serialized = new SerializedObject(staff);
            SerializedProperty distance = serialized.FindProperty("_dropOffStandDistance");
            if (distance == null)
            {
                report.Add("staff stand point skipped: _dropOffStandDistance missing");
                return;
            }

            float before = distance.floatValue;
            distance.floatValue = Mathf.Max(before, 1.6f);

            SerializedProperty idle = serialized.FindProperty("_idlePoint");
            string idleNote = string.Empty;
            if (idle != null)
            {
                Vector3 idleBefore = idle.vector3Value;
                // The old idle point sat inside the counter's shell, so the staff
                // timed out walking to it after every cycle.
                if (Mathf.Abs(idleBefore.z + 0.9f) > 0.05f)
                {
                    idle.vector3Value = new Vector3(idleBefore.x, idleBefore.y, -0.9f);
                    idleNote = $", idle point {idleBefore.ToString("0.00")} -> {idle.vector3Value.ToString("0.00")}";
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            report.Add($"staff drop-off stand distance {before:0.00} -> {distance.floatValue:0.00}{idleNote}");
        }

        /// <summary>
        /// The sealed door rotates around its own origin (the leaf centre), so
        /// opening it swung across the passage. Reparenting into a hinge does not
        /// survive the room prefab, so the controller instead rotates the leaf
        /// around a local hinge point on its south edge.
        /// </summary>
        private static void RepairSecretDoorPivot(List<string> report)
        {
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>(FindObjectsInactive.Include);
            if (room == null)
            {
                report.Add("secret door skipped: no SecretRoomController");
                return;
            }

            var serialized = new SerializedObject(room);
            SerializedProperty doorProperty = serialized.FindProperty("_sealedDoor");
            var door = doorProperty.objectReferenceValue as GameObject;

            if (door != null && door.name == "SecretDoorHinge")
            {
                var leaf = GameObject.Find("SecretDoor_Foreshadowed");
                if (leaf != null)
                {
                    doorProperty.objectReferenceValue = leaf;
                    report.Add("restored _sealedDoor to the leaf (a hinge reparent does not survive the room prefab)");
                }
            }

            var strayHinge = GameObject.Find("SecretDoorHinge");
            if (strayHinge != null)
            {
                Object.DestroyImmediate(strayHinge);
                report.Add("removed the unused SecretDoorHinge object");
            }

            SerializedProperty hinge = serialized.FindProperty("_doorHingeLocal");
            if (hinge != null && hinge.vector3Value.z < 0.5f)
            {
                hinge.vector3Value = new Vector3(0f, 0f, BackRoomGeometry.DoorHingeLocalZ);
                report.Add("door hinge point moved to the leaf's north edge");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Cuts the shop's east wall where the back-room doorway is. The back
        /// room shell already has a 1.6 m gap in its west wall, but the shop wall
        /// behind the decorative door was solid, so the passage never opened.
        /// </summary>
        private static void RepairPassageGap(List<string> report)
        {
            Transform wall = GameObject.Find("Wall_East") != null ? GameObject.Find("Wall_East").transform : null;
            if (wall == null)
            {
                report.Add("passage gap skipped: Wall_East not found");
                return;
            }

            var renderer = wall.GetComponent<Renderer>();
            Bounds bounds = GetBounds(wall.gameObject);
            GameObject doorLeaf = GameObject.Find("SecretDoor_Foreshadowed");
            float doorZ = doorLeaf != null ? doorLeaf.transform.position.z : 3.0f;
            float gapMin = BackRoomGeometry.GapMin(doorZ);
            float gapMax = BackRoomGeometry.GapMax(doorZ);
            const float gapTop = 2.4f;
            Material material = renderer != null ? renderer.sharedMaterial : null;

            if (bounds.max.z <= gapMin + 0.05f)
            {
                report.Add("passage gap already cut");
            }
            else
            {
                float southSize = gapMin - bounds.min.z;

                // Keep the original object as the south segment.
                wall.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z + southSize * 0.5f);
                wall.localScale = new Vector3(bounds.size.x, bounds.size.y, southSize);

                CreateWallSegment(
                    wall,
                    "Wall_East_North",
                    new Vector3(bounds.center.x, bounds.center.y, (gapMax + bounds.max.z) * 0.5f),
                    new Vector3(bounds.size.x, bounds.size.y, bounds.max.z - gapMax),
                    material);
                CreateWallSegment(
                    wall,
                    "Wall_East_Header",
                    new Vector3(bounds.center.x, (gapTop + bounds.max.y) * 0.5f, (gapMin + gapMax) * 0.5f),
                    new Vector3(bounds.size.x, bounds.max.y - gapTop, gapMax - gapMin),
                    material);

                report.Add($"cut a {gapMax - gapMin:0.0} m doorway in Wall_East at z={gapMin:0.0}..{gapMax:0.0}");
            }

            // The shop floor ends at the wall and the back-room floor starts
            // behind it, leaving a seam the player drops through. Bridge it.
            if (GameObject.Find("BackRoomThreshold") == null)
            {
                CreateWallSegment(
                    wall,
                    "BackRoomThreshold",
                    new Vector3(bounds.center.x, -0.03f, (gapMin + gapMax) * 0.5f),
                    new Vector3(0.6f, 0.06f, gapMax - gapMin),
                    material);
                report.Add("bridged the shop/back-room floor seam with BackRoomThreshold");
            }
        }

        private static void CreateWallSegment(Transform sibling, string name, Vector3 position, Vector3 size,
            Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(sibling.parent, false);
            cube.transform.position = position;
            cube.transform.localScale = size;
            if (material != null)
            {
                cube.GetComponent<Renderer>().sharedMaterial = material;
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

        private static void RepairAmbientDropOff(List<string> report)
        {
            int cleared = 0;
            foreach (AmbientNpc npc in Object.FindObjectsByType<AmbientNpc>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(npc);
                SerializedProperty slot = serialized.FindProperty("_dropOffSlot");
                if (slot == null || slot.objectReferenceValue == null)
                {
                    continue;
                }

                slot.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                cleared++;
            }

            report.Add($"ambient drop-off links cleared: {cleared}");
        }

        /// <summary>
        /// Batch-mode gate. Throws on any failure so the Unity process exits
        /// non-zero; prints one line per check.
        /// </summary>
        [MenuItem("Laundry Legends/Validate Playtest Wiring")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var failures = new List<string>();
            var checkedItems = new List<string>();

            // HUD.
            OrderTicketView ticket = Object.FindFirstObjectByType<OrderTicketView>(FindObjectsInactive.Include);
            Require(ticket != null, "OrderTicketView is missing from the scene.", failures);
            if (ticket != null)
            {
                Require(ticket.gameObject.activeInHierarchy, "The HUD object is inactive.", failures);
                Require(ticket.enabled, "OrderTicketView is disabled.", failures);
                Require(ticket.Spawner != null && ticket.Spawner.GetComponent<CustomerSpawner>() != null,
                    "OrderTicketView has no CustomerSpawner.", failures);
                checkedItems.Add("HUD order ticket");
            }

            // Player interaction prompt.
            InteractionPromptView prompt = Object.FindFirstObjectByType<InteractionPromptView>(FindObjectsInactive.Include);
            Require(prompt != null, "InteractionPromptView is missing from the player.", failures);
            if (prompt != null)
            {
                Require(prompt.gameObject.activeInHierarchy, "The player prompt object is inactive.", failures);
                Require(prompt.enabled, "InteractionPromptView is disabled.", failures);
                var serialized = new SerializedObject(prompt);
                Require(serialized.FindProperty("_interactor").objectReferenceValue != null,
                    "InteractionPromptView has no PlayerInteractor.", failures);
                checkedItems.Add("interaction prompt");
            }

            PlayerInteractor[] interactors = Object.FindObjectsByType<PlayerInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(interactors.Length == 1, $"Expected one PlayerInteractor, found {interactors.Length}.", failures);
            if (interactors.Length == 1)
            {
                var serialized = new SerializedObject(interactors[0]);
                SerializedProperty camera = serialized.FindProperty("_camera");
                Require(camera.objectReferenceValue is Camera, "PlayerInteractor has no camera assigned.", failures);
                SerializedProperty range = serialized.FindProperty("_range");
                Require(range != null && range.floatValue > 0f, "PlayerInteractor has no positive range.", failures);
                checkedItems.Add("player interactor camera");
            }

            // Order pipeline.
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            Require(spawner != null, "CustomerSpawner is missing.", failures);
            if (spawner != null)
            {
                var serialized = new SerializedObject(spawner);
                foreach (string field in new[] { "_customerPrefab", "_laundryPrefab", "_orderSlot", "_streetPoint", "_doorPoint", "_counterPoint", "_exitPoint" })
                {
                    Require(serialized.FindProperty(field).objectReferenceValue != null,
                        $"CustomerSpawner.{field} is not assigned.", failures);
                }

                checkedItems.Add("customer spawner");

                var counterPoint = serialized.FindProperty("_counterPoint").objectReferenceValue as Transform;
                Require(counterPoint != null, "CustomerSpawner._counterPoint is not assigned.", failures);
                if (counterPoint != null)
                {
                    Require(
                        !IsCustomerBodyBlocked(counterPoint.position),
                        $"The customer counter point {counterPoint.position.ToString("0.00")} is inside furniture; " +
                        "the customer can never stand there.",
                        failures);
                    checkedItems.Add("customer counter point clear");
                }
            }

            Require(GameObject.Find("DropOffSlot") != null,
                "DropOffSlot is missing, so the staff pipeline has nothing to pick up.", failures);

            AmbientNpc[] ambientNpcs = Object.FindObjectsByType<AmbientNpc>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AmbientNpc ambient in ambientNpcs)
            {
                var serializedNpc = new SerializedObject(ambient);
                SerializedProperty dropOff = serializedNpc.FindProperty("_dropOffSlot");
                Require(dropOff == null || dropOff.objectReferenceValue == null,
                    $"Ambient NPC '{ambient.name}' still feeds the order drop-off, so decorative laundry starves real orders.",
                    failures);
            }

            if (ambientNpcs.Length > 0)
            {
                checkedItems.Add("ambient NPCs stay out of the order slot");
            }


            LaundryStaff staff = Object.FindFirstObjectByType<LaundryStaff>(FindObjectsInactive.Include);
            Require(staff != null && staff.isActiveAndEnabled, "LaundryStaff is missing or disabled.", failures);
            if (staff != null)
            {
                var staffSerialized = new SerializedObject(staff);
                SerializedProperty standDistance = staffSerialized.FindProperty("_dropOffStandDistance");
                Require(
                    standDistance != null && standDistance.floatValue >= 1.5f,
                    "The staff drop-off stand point is too close to the counter; overlap correction will pin the staff.",
                    failures);
            }

            checkedItems.Add("staff drop-off");

            // Back-room purchase.
            HiddenRoomPurchasePoint purchase = Object.FindFirstObjectByType<HiddenRoomPurchasePoint>(FindObjectsInactive.Include);
            Require(purchase != null, "HiddenRoomPurchasePoint is missing.", failures);
            if (purchase != null)
            {
                var serialized = new SerializedObject(purchase);
                Require(serialized.FindProperty("_room").objectReferenceValue != null,
                    "HiddenRoomPurchasePoint has no SecretRoomController.", failures);
                Require(serialized.FindProperty("_spawner").objectReferenceValue != null,
                    "HiddenRoomPurchasePoint has no CustomerSpawner, so it has no wallet.", failures);

                Collider[] colliders = purchase.GetComponentsInChildren<Collider>(true);
                Require(colliders.Length > 0, "HiddenRoomPurchasePoint has no collider in its hierarchy.", failures);

                Transform anchor = purchase.transform;
                foreach (Collider collider in colliders)
                {
                    if (collider.name == "SecretAccess_Shelf" || collider.name == "SecretAccess_ServicePanel")
                    {
                        anchor = collider.transform;
                        break;
                    }
                }

                Vector3 target = new Vector3(anchor.position.x, 1.2f, anchor.position.z);
                var approach = new Vector3(anchor.position.x - 1.4f, 1.6f, anchor.position.z);
                Ray ray = new Ray(approach, (target - approach).normalized);
                IInteractable hitInteractable = null;
                if (Physics.Raycast(ray, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    hitInteractable = hit.collider.GetComponentInParent<IInteractable>();
                }

                Require(ReferenceEquals(hitInteractable, purchase),
                    "Looking at the shelf from 1.4 m does not target the purchase point.", failures);
                checkedItems.Add("shelf purchase reachable");

                SecretRoomController room = serialized.FindProperty("_room").objectReferenceValue as SecretRoomController;
                if (room != null)
                {
                    var serializedRoom = new SerializedObject(room);
                    GameObject cover = serializedRoom.FindProperty("_sealedDoorCover").objectReferenceValue as GameObject;
                    Require(cover != null && cover == purchase.gameObject,
                        "The sealed-door cover is not the shelf, so paying would not clear the doorway.", failures);
                    checkedItems.Add("shelf clears the doorway");

                    var sealedDoor = serializedRoom.FindProperty("_sealedDoor").objectReferenceValue as GameObject;
                    Require(
                        sealedDoor != null && sealedDoor.name == "SecretDoor_Foreshadowed",
                        "The sealed door reference points at the wrong object.",
                        failures);

                    SerializedProperty hinge = serializedRoom.FindProperty("_doorHingeLocal");
                    Require(
                        hinge != null && hinge.vector3Value.z >= 0.5f,
                        "The sealed door hinge point is not on the leaf edge, so opening it would swing across the passage.",
                        failures);
                    checkedItems.Add("secret door hinged clear of the passage");

                    Physics.SyncTransforms();
                    bool wallBlocks = false;
                    foreach (RaycastHit passageHit in Physics.RaycastAll(
                                 new Ray(new Vector3(6.3f, 1.2f, 3.0f), Vector3.right),
                                 2.5f,
                                 ~0,
                                 QueryTriggerInteraction.Ignore))
                    {
                        if (passageHit.collider != null && passageHit.collider.name == "Wall_East")
                        {
                            wallBlocks = true;
                        }
                    }

                    Require(!wallBlocks, "The shop's east wall still blocks the back-room doorway.", failures);
                    checkedItems.Add("back-room passage open");
                }
            }

            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException(
                    "[VALIDATE] Playtest wiring failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log($"[VALIDATE] Playtest wiring passed: {string.Join(", ", checkedItems)}.");
        }

        private static void Require(bool condition, string message, List<string> failures)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
