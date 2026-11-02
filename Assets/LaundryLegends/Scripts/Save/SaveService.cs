using System;
using System.IO;
using LaundryLegends.Arcade;
using LaundryLegends.Audio;
using LaundryLegends.Business;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Progression;
using UnityEngine;

namespace LaundryLegends.Save
{
    /// <summary>
    /// JSON save/load for the laundromat progression. Writes atomically through a
    /// temporary file and treats a missing/corrupt file as "no save".
    /// </summary>
    public static class SaveService
    {
        private const string FileName = "laundry-legends.save.json";
        private static string _overridePath;

        public static string FilePath => string.IsNullOrEmpty(_overridePath)
            ? Path.Combine(Application.persistentDataPath, FileName)
            : _overridePath;

        public static bool HasSave => File.Exists(FilePath);

        /// <summary>Test hook: point the store at a temporary file.</summary>
        public static void OverridePath(string path)
        {
            _overridePath = path;
        }

        public static SaveData Capture(
            CustomerSpawner spawner,
            BusinessClock clock,
            ConsumableStockService stock,
            LaundryReputation reputation,
            SecretRoomController room,
            ArcadeService arcade)
        {
            var data = new SaveData
            {
                Money = spawner != null && spawner.Economy != null ? spawner.Economy.Balance : 0,
                Day = clock != null ? clock.Day : 1,
                MinuteOfDay = clock != null ? clock.MinuteOfDay : 480,
                Reputation = reputation != null ? reputation.Value : 20,
                RoomUnlocked = room != null && room.IsRevealed,
                ArcadeExpanded = arcade != null && arcade.IsExpanded,
                MasterVolume = GameAudioSettings.Master,
                MusicVolume = GameAudioSettings.Music,
                SfxVolume = GameAudioSettings.Sfx
            };

            if (stock != null)
            {
                data.Detergent = stock.Count(ConsumableType.Detergent);
                data.Fragrance = stock.Count(ConsumableType.Fragrance);
                data.Plastic = stock.Count(ConsumableType.Plastic);
            }

            return data;
        }

        public static bool Save(SaveData data)
        {
            if (data == null)
            {
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }

                File.Move(temp, FilePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SAVE] Could not write {FilePath}: {exception.Message}");
                return false;
            }
        }

        /// <summary>Returns null on a missing or unreadable file; never throws.</summary>
        public static SaveData Load()
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data == null || data.Version <= 0)
                {
                    Debug.LogWarning($"[SAVE] Ignoring invalid save at {FilePath}.");
                    return null;
                }

                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SAVE] Corrupt save at {FilePath}: {exception.Message}");
                return null;
            }
        }

        public static void Apply(
            SaveData data,
            CustomerSpawner spawner,
            BusinessClock clock,
            ConsumableStockService stock,
            LaundryReputation reputation,
            SecretRoomController room,
            ArcadeService arcade)
        {
            if (data == null)
            {
                return;
            }

            if (spawner != null && spawner.Economy != null)
            {
                spawner.Economy.RestoreBalance(data.Money);
            }

            if (clock != null)
            {
                clock.State.Set(Mathf.Max(1, data.Day), Mathf.Clamp(data.MinuteOfDay, 0, 1439));
            }

            if (stock != null)
            {
                SetStock(stock, ConsumableType.Detergent, data.Detergent);
                SetStock(stock, ConsumableType.Fragrance, data.Fragrance);
                SetStock(stock, ConsumableType.Plastic, data.Plastic);
            }

            if (reputation != null)
            {
                reputation.SetValue(data.Reputation);
            }

            if (data.RoomUnlocked && room != null && !room.IsRevealed)
            {
                room.UnlockByPurchase();
            }

            if (data.ArcadeExpanded && arcade != null)
            {
                arcade.RestoreExpansion(true);
            }

            GameAudioSettings.SetMaster(data.MasterVolume);
            GameAudioSettings.SetMusic(data.MusicVolume);
            GameAudioSettings.SetSfx(data.SfxVolume);
        }

        private static void SetStock(ConsumableStockService stock, ConsumableType type, int count)
        {
            ConsumableShelf shelf = stock.Find(type);
            if (shelf != null)
            {
                shelf.SetCount(Mathf.Clamp(count, 0, shelf.Capacity));
            }
        }
    }
}
