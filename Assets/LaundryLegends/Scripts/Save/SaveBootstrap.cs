using LaundryLegends.Arcade;
using LaundryLegends.Business;
using LaundryLegends.Customers;
using LaundryLegends.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Save
{
    /// <summary>
    /// Loads the save when the Laundromat scene opens and autosaves every minute
    /// and on quit. Skipped in batch mode so tests never read the player's file.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaveBootstrap : MonoBehaviour
    {
        private static SaveBootstrap _instance;
        private const float AutosaveSeconds = 60f;

        public bool LoadedThisSession { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Application.isBatchMode || _instance != null)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name != "Laundromat")
            {
                return;
            }

            var host = new GameObject("SaveBootstrap");
            _instance = host.AddComponent<SaveBootstrap>();
            DontDestroyOnLoad(host);
        }

        private void Start()
        {
            LoadNow();
            InvokeRepeating(nameof(SaveNow), AutosaveSeconds, AutosaveSeconds);
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        public void LoadNow()
        {
            SaveData data = SaveService.Load();
            if (data == null)
            {
                return;
            }

            SaveService.Apply(
                data,
                Object.FindFirstObjectByType<CustomerSpawner>(),
                Object.FindFirstObjectByType<BusinessClock>(),
                Object.FindFirstObjectByType<ConsumableStockService>(),
                Object.FindFirstObjectByType<LaundryReputation>(),
                Object.FindFirstObjectByType<SecretRoomController>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<ArcadeService>(FindObjectsInactive.Include));
            LoadedThisSession = true;
            Debug.Log($"[SAVE] Loaded day {data.Day}, {data.MinuteOfDay / 60:00}:{data.MinuteOfDay % 60:00}, ${data.Money}.");
        }

        public void SaveNow()
        {
            SaveData data = SaveService.Capture(
                Object.FindFirstObjectByType<CustomerSpawner>(),
                Object.FindFirstObjectByType<BusinessClock>(),
                Object.FindFirstObjectByType<ConsumableStockService>(),
                Object.FindFirstObjectByType<LaundryReputation>(),
                Object.FindFirstObjectByType<SecretRoomController>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<ArcadeService>(FindObjectsInactive.Include));
            SaveService.Save(data);
        }
    }
}
