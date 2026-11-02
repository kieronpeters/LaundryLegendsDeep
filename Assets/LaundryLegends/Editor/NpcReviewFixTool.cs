using LaundryLegends.Customers;
using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Phase 11 NPC review fixes: move the phone regular out of the waiting-chair
    /// debris, move the ambient customer's counter waypoint into the waiting area,
    /// and give the male order customer the existing male DropOff voice clips.
    /// </summary>
    public static class NpcReviewFixTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string BasePrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Regular.prefab";
        private const string OrderPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_OrderCustomer.prefab";
        private const string VoiceLibrary = "Assets/LaundryLegends/ScriptableObjects/SO_DialogueVoiceLibrary.asset";

        [MenuItem("Laundry Legends/Phase 11/Apply NPC Review Fixes")]
        public static void ApplyReviewFixes()
        {
            GameObject orderPrefab = CreateOrderCustomerPrefab();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            if (spawner != null && orderPrefab != null)
            {
                var serialized = new SerializedObject(spawner);
                SerializedProperty customer = serialized.FindProperty("_customerPrefab");
                if (customer != null)
                {
                    customer.objectReferenceValue = orderPrefab;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GameObject phone = GameObject.Find("PhoneRegular");
            if (phone != null)
            {
                phone.transform.position = new Vector3(6.0f, 0f, -4.2f);
                foreach (PickupCustomer pickup in phone.GetComponentsInChildren<PickupCustomer>(true))
                {
                    Object.DestroyImmediate(pickup);
                }
            }

            GameObject waypoint = GameObject.Find("Waypoint_Counter");
            if (waypoint != null)
            {
                waypoint.name = "Waypoint_Waiting";
                waypoint.transform.position = new Vector3(4.3f, 0f, -4.2f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NPC-FIX] PhoneRegular moved, ambient waiting waypoint moved, male order-customer voice prefab wired.");
        }

        private static GameObject CreateOrderCustomerPrefab()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(BasePrefab);
            try
            {
                var audio = contents.GetComponent<AudioSource>();
                if (audio == null)
                {
                    audio = contents.AddComponent<AudioSource>();
                }

                audio.playOnAwake = false;
                audio.spatialBlend = 0f;
                audio.volume = 0.9f;

                var dialogue = contents.GetComponent<NpcDialogue>();
                if (dialogue == null)
                {
                    dialogue = contents.AddComponent<NpcDialogue>();
                }

                dialogue.ConfigureForBank(NpcArchetype.DropOff, DialogueTrigger.Enter);
                dialogue.SetAmbientEnabled(false);

                var voice = contents.GetComponent<DialogueVoicePlayer>();
                if (voice == null)
                {
                    voice = contents.AddComponent<DialogueVoicePlayer>();
                }

                var library = AssetDatabase.LoadAssetAtPath<DialogueVoiceLibrary>(VoiceLibrary);
                voice.Configure(
                    NpcArchetype.DropOff,
                    library,
                    audio,
                    dialogue,
                    new[] { DialogueTrigger.Enter, DialogueTrigger.Counter, DialogueTrigger.Pickup },
                    autoSpeak: false);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, OrderPrefab);
                return saved;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
