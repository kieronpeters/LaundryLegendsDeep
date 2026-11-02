using System;

namespace LaundryLegends.Save
{
    /// <summary>
    /// Versioned save payload. Only stable, player-visible progression is stored;
    /// transient order/customer state is recreated on load.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Money;
        public int Day = 1;
        public int MinuteOfDay = 480;
        public int Reputation = 20;
        public bool RoomUnlocked;
        public bool ArcadeExpanded;
        public int Detergent;
        public int Fragrance;
        public int Plastic;
        public float MasterVolume = 0.8f;
        public float MusicVolume = 0.6f;
        public float SfxVolume = 0.8f;
    }
}
