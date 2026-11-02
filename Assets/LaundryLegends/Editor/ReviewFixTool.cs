using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.NPC;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Owner-review scene fixes:
    /// - remove the female arcade visitor that clipped into the hidden-room wall,
    /// - switch the order customer to the male regular model,
    /// - remove the staff's laundry commentary.
    /// Idempotent; safe to re-run.
    /// </summary>
    public static class ReviewFixTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string MaleCustomerPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Regular.prefab";

        [MenuItem("Laundry Legends/Phase 9/Apply Review NPC Fixes")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ArcadeTraffic traffic = Object.FindFirstObjectByType<ArcadeTraffic>(FindObjectsInactive.Include);
            if (traffic != null)
            {
                var serialized = new SerializedObject(traffic);
                SerializedProperty visitor = serialized.FindProperty("_visitorPrefab");
                if (visitor != null)
                {
                    visitor.objectReferenceValue = null;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning("[REVIEW] No ArcadeTraffic found; hidden-room visitor unchanged.");
            }

            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            GameObject maleCustomer = AssetDatabase.LoadAssetAtPath<GameObject>(MaleCustomerPrefab);
            if (spawner != null && maleCustomer != null)
            {
                var serialized = new SerializedObject(spawner);
                SerializedProperty customer = serialized.FindProperty("_customerPrefab");
                if (customer != null)
                {
                    customer.objectReferenceValue = maleCustomer;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning("[REVIEW] CustomerSpawner or male customer prefab missing.");
            }

            LaundryStaff staff = Object.FindFirstObjectByType<LaundryStaff>(FindObjectsInactive.Include);
            if (staff != null)
            {
                foreach (NpcDialogue dialogue in staff.GetComponentsInChildren<NpcDialogue>(true))
                {
                    Object.DestroyImmediate(dialogue);
                }

                foreach (DialogueVoicePlayer voice in staff.GetComponentsInChildren<DialogueVoicePlayer>(true))
                {
                    Object.DestroyImmediate(voice);
                }
            }
            else
            {
                Debug.LogWarning("[REVIEW] No LaundryStaff found; staff commentary unchanged.");
            }

            // Purchases live in the U menu now. Hide the old ground pedestals; the
            // menu reads their upgrade data even while the objects are inactive.
            foreach (ArcadeInstallPoint point in Object.FindObjectsByType<ArcadeInstallPoint>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (point != null)
                {
                    point.gameObject.SetActive(false);
                }
            }

            foreach (ArcadeUpgradePoint point in Object.FindObjectsByType<ArcadeUpgradePoint>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (point != null)
                {
                    point.gameObject.SetActive(false);
                }
            }

            foreach (ArcadeExpansionPoint point in Object.FindObjectsByType<ArcadeExpansionPoint>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (point != null)
                {
                    point.gameObject.SetActive(false);
                }
            }

            GameObject hud = GameObject.Find("HUD");
            if (hud == null)
            {
                var ticket = Object.FindFirstObjectByType<OrderTicketView>(FindObjectsInactive.Include);
                hud = ticket != null ? ticket.gameObject : null;
            }

            if (hud != null && hud.GetComponent<PurchaseMenuView>() == null)
            {
                hud.AddComponent<PurchaseMenuView>();
            }
            else if (hud == null)
            {
                Debug.LogWarning("[REVIEW] No HUD found; purchase menu not added.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[REVIEW] Visitor woman removed, order customer is male, staff commentary silenced, purchase menu added.");
        }
    }
}
