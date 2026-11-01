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
        /// The ambient customer is scenery. Anything it places in the order
        /// drop-off is processed by the staff as the next order, ahead of a real
        /// customer, so the link is cleared instead of repointed.
        /// </summary>
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
