using LaundryLegends.Customers;
using LaundryLegends.NPC;
using LaundryLegends.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>Diagnostic dump for the "orders stop in the back room" report.</summary>
    public static class SceneAuditTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Diagnostics/Audit Order Stall")]
        public static void AuditOrderStall()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[AUDIT] === order-stall audit ===");
            LogByName("CustomerSpawner");
            LogByName("CustomerLane");
            LogByName("AmbientActors");
            LogByName("Attendant");
            LogByName("PhoneRegular");
            LogByName("StreetCustomer");
            LogByName("DropOffSlot");
            LogByName("FoldingTable");
            LogByName("GreyboxRoom");
            LogByName("RoomContent");
            LogByName("BackRoomShell");
            LogByName("HUD");
            LogByName("Player");

            var staff = Object.FindFirstObjectByType<LaundryStaff>(FindObjectsInactive.Include);
            LogObject("LaundryStaff", staff != null ? staff.gameObject : null);
            if (staff != null)
            {
                Debug.Log($"[AUDIT] staff enabled={staff.enabled} activeAndEnabled={staff.isActiveAndEnabled}");
                var staffSerialized = new SerializedObject(staff);
                LogReference(staffSerialized, "_orderRack");
            }

            var rack = Object.FindFirstObjectByType<LaundryLegends.Business.DropOffRack>(
                FindObjectsInactive.Include);
            if (rack != null)
            {
                LogObject("DropOffRack", rack.gameObject);
                Debug.Log($"[AUDIT] rack capacity={rack.Capacity} occupied={rack.OccupiedCount}");
                for (int i = 0; i < rack.Slots.Count; i++)
                {
                    LogObject($"RackSlot_{i}", rack.Slots[i] != null ? rack.Slots[i].gameObject : null);
                }
            }

            var spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            if (spawner != null)
            {
                var serialized = new SerializedObject(spawner);
                LogReference(serialized, "_customerPrefab");
                LogReference(serialized, "_orderSlot");
                Debug.Log($"[AUDIT] spawner autoStart={serialized.FindProperty("_autoStart")?.boolValue} respawn={serialized.FindProperty("_respawnDelay")?.floatValue}");
            }

            var room = Object.FindFirstObjectByType<SecretRoomController>(FindObjectsInactive.Include);
            if (room != null)
            {
                var serialized = new SerializedObject(room);
                LogReference(serialized, "_roomContent");
                LogReference(serialized, "_sealedDoor");
                LogReference(serialized, "_sealedDoorCover");
                Debug.Log($"[AUDIT] room stage={room.Stage}");
            }

            Debug.Log("[AUDIT] === end ===");
        }

        private static void LogByName(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
            {
                foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (candidate != null && candidate.name == name && candidate.scene.IsValid())
                    {
                        root = candidate;
                        break;
                    }
                }
            }

            LogObject(name, root);
        }

        private static void LogObject(string label, GameObject go)
        {
            if (go == null)
            {
                Debug.Log($"[AUDIT] {label}: MISSING");
                return;
            }

            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            Debug.Log($"[AUDIT] {label}: {path} activeSelf={go.activeSelf} activeInHierarchy={go.activeInHierarchy} pos={go.transform.position}");
        }

        private static void LogReference(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.Log($"[AUDIT] {propertyName}: field missing");
                return;
            }

            var target = property.objectReferenceValue as GameObject;
            if (target != null)
            {
                LogObject(propertyName, target);
            }
            else
            {
                var component = property.objectReferenceValue as Component;
                LogObject(propertyName, component != null ? component.gameObject : null);
            }
        }
    }
}
