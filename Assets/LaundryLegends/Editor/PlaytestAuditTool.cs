using System.Collections.Generic;
using System.Text;
using LaundryLegends.Customers;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using LaundryLegends.Player;
using LaundryLegends.Progression;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Read-only audit of the shipped Laundromat scene. It reports the wiring the
    /// Phase 8 playtest depended on: HUD views, player interaction, the customer
    /// spawner, the staff drop-off/machine/folding pipeline, and the back-room
    /// purchase point. It never saves the scene.
    /// </summary>
    public static class PlaytestAuditTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Audit Playtest Wiring")]
        public static void Audit()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var report = new StringBuilder();
            report.AppendLine("[AUDIT] ---- Laundromat playtest wiring ----");

            AuditHud(report);
            AuditPlayer(report);
            AuditSpawner(report);
            AuditStaff(report);
            AuditShelf(report);
            AuditShelfReachability(report);

            report.AppendLine("[AUDIT] ---- end ----");
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Samples eye-level rays from a grid of standing positions in front of
        /// the shelf and reports whether the first hit resolves to the purchase
        /// point. This is what the player's interaction ray actually does.
        /// </summary>
        private static void AuditShelfReachability(StringBuilder report)
        {
            HiddenRoomPurchasePoint point = Object.FindAnyObjectByType<HiddenRoomPurchasePoint>();
            if (point == null)
            {
                report.AppendLine("[AUDIT] reachability: no purchase point");
                return;
            }

            Bounds target;
            Collider panel = point.GetComponent<Collider>();
            if (panel == null)
            {
                report.AppendLine("[AUDIT] reachability: purchase point has no collider on its own object");
                return;
            }

            target = panel.bounds;
            Vector3 aim = new Vector3(target.center.x, 1.3f, target.center.z);

            int attempts = 0;
            int resolved = 0;
            var firstBlockers = new List<string>();
            for (float x = 4.6f; x <= 7.6f; x += 0.3f)
            {
                for (float z = 1.4f; z <= 4.6f; z += 0.4f)
                {
                    var eye = new Vector3(x, 1.6f, z);
                    Vector3 direction = aim - eye;
                    float distance = direction.magnitude;
                    if (distance > 3f)
                    {
                        continue;
                    }

                    attempts++;
                    if (Physics.Raycast(eye, direction.normalized, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        IInteractable hitInteractable = hit.collider.GetComponentInParent<IInteractable>();
                        if (ReferenceEquals(hitInteractable, point))
                        {
                            resolved++;
                        }
                        else
                        {
                            string description = $"{hit.collider.name}/{hit.collider.GetType().Name} at {hit.distance:0.00}m -> {(ReferenceEquals(hitInteractable, null) ? "no interactable" : hitInteractable.GetType().Name)}";
                            if (!firstBlockers.Contains(description))
                            {
                                firstBlockers.Add(description);
                            }
                        }
                    }
                    else
                    {
                        const string miss = "no hit";
                        if (!firstBlockers.Contains(miss))
                        {
                            firstBlockers.Add(miss);
                        }
                    }
                }
            }

            report.AppendLine($"[AUDIT] reachability: shelf hit by {resolved}/{attempts} eye-level samples within 3 m");
            foreach (string blocker in firstBlockers)
            {
                report.AppendLine($"[AUDIT]   blocked by: {blocker}");
            }

            AuditShelfNeighbours(report, aim);
        }

        /// <summary>Lists every collider within 3 m of the shelf, to show what sits in front.</summary>
        private static void AuditShelfNeighbours(StringBuilder report, Vector3 centre)
        {
            Collider[] nearby = Physics.OverlapSphere(centre, 3f, ~0, QueryTriggerInteraction.Ignore);
            report.AppendLine($"[AUDIT] colliders within 3 m of the shelf: {nearby.Length}");
            foreach (Collider collider in nearby)
            {
                report.AppendLine(
                    $"[AUDIT]   {Path(collider.transform)} / {collider.GetType().Name} trigger={collider.isTrigger} enabled={collider.enabled} boundsCenter={collider.bounds.center}");
            }
        }

        private static void AuditHud(StringBuilder report)
        {
            OrderTicketView[] tickets = Object.FindObjectsByType<OrderTicketView>(FindObjectsSortMode.None);
            report.AppendLine($"[AUDIT] HUD OrderTicketView count={tickets.Length}");
            foreach (OrderTicketView ticket in tickets)
            {
                report.AppendLine(
                    $"[AUDIT]   ticket '{Path(ticket.transform)}' activeInHierarchy={ticket.gameObject.activeInHierarchy} enabled={ticket.enabled} spawner={(ticket.Spawner != null ? ticket.Spawner.name : "NULL")}");
            }
        }

        private static void AuditPlayer(StringBuilder report)
        {
            PlayerInteractor[] interactors = Object.FindObjectsByType<PlayerInteractor>(FindObjectsSortMode.None);
            InteractionPromptView[] prompts = Object.FindObjectsByType<InteractionPromptView>(FindObjectsSortMode.None);
            CarryController[] carries = Object.FindObjectsByType<CarryController>(FindObjectsSortMode.None);
            report.AppendLine($"[AUDIT] Player interactors={interactors.Length} prompts={prompts.Length} carry={carries.Length}");

            foreach (PlayerInteractor interactor in interactors)
            {
                var serialized = new SerializedObject(interactor);
                SerializedProperty camera = serialized.FindProperty("_camera");
                SerializedProperty range = serialized.FindProperty("_range");
                SerializedProperty mask = serialized.FindProperty("_interactionMask");
                report.AppendLine(
                    $"[AUDIT]   interactor '{Path(interactor.transform)}' activeInHierarchy={interactor.gameObject.activeInHierarchy} enabled={interactor.enabled} camera={(camera.objectReferenceValue != null ? camera.objectReferenceValue.name : "NULL")} range={(range != null ? range.floatValue : -1)} mask={(mask != null ? mask.intValue : 0)} key={interactor.InteractKey}");
            }

            foreach (InteractionPromptView prompt in prompts)
            {
                var serialized = new SerializedObject(prompt);
                SerializedProperty interactor = serialized.FindProperty("_interactor");
                SerializedProperty carry = serialized.FindProperty("_carry");
                report.AppendLine(
                    $"[AUDIT]   prompt '{Path(prompt.transform)}' activeInHierarchy={prompt.gameObject.activeInHierarchy} enabled={prompt.enabled} interactor={(interactor.objectReferenceValue != null ? interactor.objectReferenceValue.name : "NULL")} carry={(carry.objectReferenceValue != null ? carry.objectReferenceValue.name : "NULL")}");
            }
        }

        private static void AuditSpawner(StringBuilder report)
        {
            CustomerSpawner[] spawners = Object.FindObjectsByType<CustomerSpawner>(FindObjectsSortMode.None);
            report.AppendLine($"[AUDIT] CustomerSpawner count={spawners.Length}");
            foreach (CustomerSpawner spawner in spawners)
            {
                var serialized = new SerializedObject(spawner);
                report.AppendLine(
                    $"[AUDIT]   spawner '{Path(spawner.transform)}' enabled={spawner.enabled} prefab={Ref(serialized, "_customerPrefab")} laundry={Ref(serialized, "_laundryPrefab")} slot={Ref(serialized, "_orderSlot")} street={Ref(serialized, "_streetPoint")} door={Ref(serialized, "_doorPoint")} counter={Ref(serialized, "_counterPoint")} exit={Ref(serialized, "_exitPoint")} autoStart={serialized.FindProperty("_autoStart").boolValue} price={serialized.FindProperty("_price").intValue}");
            }
        }

        private static void AuditStaff(StringBuilder report)
        {
            LaundryStaff[] staff = Object.FindObjectsByType<LaundryStaff>(FindObjectsSortMode.None);
            report.AppendLine($"[AUDIT] LaundryStaff count={staff.Length}");
            foreach (LaundryStaff member in staff)
            {
                report.AppendLine(
                    $"[AUDIT]   staff '{Path(member.transform)}' activeInHierarchy={member.gameObject.activeInHierarchy} enabled={member.enabled} position={member.transform.position}");
            }

            GameObject dropOff = GameObject.Find("DropOffSlot");
            report.AppendLine($"[AUDIT] DropOffSlot found={(dropOff != null)}" +
                              (dropOff != null
                                  ? $" position={dropOff.transform.position} colliders={dropOff.GetComponentsInChildren<Collider>(true).Length} activeInHierarchy={dropOff.activeInHierarchy} slot={(dropOff.GetComponent<CarrySlot>() != null)}"
                                  : string.Empty));

            foreach (string machineName in new[] { "Machine_1", "Machine_2" })
            {
                GameObject machine = GameObject.Find(machineName);
                MachineLoadHook hook = machine != null ? machine.GetComponent<MachineLoadHook>() : null;
                report.AppendLine($"[AUDIT] {machineName} found={(machine != null)} position={(machine != null ? machine.transform.position.ToString() : "-")} hook={(hook != null)} kind={(hook != null ? hook.Kind.ToString() : "-")} slot={(hook != null && hook.Slot != null)}");
            }

            FoldingStation[] foldings = Object.FindObjectsByType<FoldingStation>(FindObjectsSortMode.None);
            report.AppendLine($"[AUDIT] FoldingStation count={foldings.Length}");
            foreach (FoldingStation folding in foldings)
            {
                var serialized = new SerializedObject(folding);
                SerializedProperty slot = serialized.FindProperty("_slot");
                report.AppendLine(
                    $"[AUDIT]   folding '{Path(folding.transform)}' position={folding.transform.position} slot={(slot.objectReferenceValue != null ? slot.objectReferenceValue.name : "NULL")} colliders={folding.GetComponentsInChildren<Collider>(true).Length}");
            }
        }

        private static void AuditShelf(StringBuilder report)
        {
            HiddenRoomPurchasePoint[] points = Object.FindObjectsByType<HiddenRoomPurchasePoint>(FindObjectsSortMode.None);
            report.AppendLine($"[AUDIT] HiddenRoomPurchasePoint count={points.Length}");
            foreach (HiddenRoomPurchasePoint point in points)
            {
                var serialized = new SerializedObject(point);
                Collider[] colliders = point.GetComponentsInChildren<Collider>(true);
                var own = new List<string>();
                foreach (Collider collider in colliders)
                {
                    own.Add($"{collider.GetType().Name}(enabled={collider.enabled},trigger={collider.isTrigger})");
                }

                report.AppendLine(
                    $"[AUDIT]   shelf '{Path(point.transform)}' activeInHierarchy={point.gameObject.activeInHierarchy} enabled={point.enabled} position={point.transform.position} room={Ref(serialized, "_room")} spawner={Ref(serialized, "_spawner")} cost={point.Cost} colliders=[{string.Join(", ", own)}]");
                if (colliders.Length > 0)
                {
                    Bounds bounds = colliders[0].bounds;
                    report.AppendLine($"[AUDIT]   shelf bounds center={bounds.center} size={bounds.size}");
                }
            }
        }

        private static string Ref(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                return "MISSING-FIELD";
            }

            Object value = property.objectReferenceValue;
            return value != null ? value.name : "NULL";
        }

        private static string Path(Transform transform)
        {
            var parts = new List<string>();
            Transform current = transform;
            while (current != null && parts.Count < 6)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
