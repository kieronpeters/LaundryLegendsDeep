using System;
using UnityEngine;

namespace LaundryLegends.Audio
{
    /// <summary>
    /// Master/music/SFX volumes with PlayerPrefs persistence. There is no native
    /// AudioMixer asset yet; the three buses are applied to the listener and to the
    /// sources owned by <see cref="GameAudio"/>.
    /// </summary>
    public static class GameAudioSettings
    {
        public const string MasterKey = "ll.audio.master";
        public const string MusicKey = "ll.audio.music";
        public const string SfxKey = "ll.audio.sfx";

        public const float DefaultMaster = 0.8f;
        public const float DefaultMusic = 0.6f;
        public const float DefaultSfx = 0.8f;

        public static float Master { get; private set; } = DefaultMaster;
        public static float Music { get; private set; } = DefaultMusic;
        public static float Sfx { get; private set; } = DefaultSfx;

        public static event Action Changed;

        static GameAudioSettings()
        {
            Load();
        }

        public static void Load()
        {
            Master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, DefaultMaster));
            Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, DefaultMusic));
            Sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultSfx));
            Apply();
        }

        public static void SetMaster(float value)
        {
            Master = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, Master);
            PlayerPrefs.Save();
            Apply();
        }

        public static void SetMusic(float value)
        {
            Music = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, Music);
            PlayerPrefs.Save();
            Apply();
        }

        public static void SetSfx(float value)
        {
            Sfx = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, Sfx);
            PlayerPrefs.Save();
            Apply();
        }

        public static void ResetToDefaults()
        {
            Master = DefaultMaster;
            Music = DefaultMusic;
            Sfx = DefaultSfx;
            PlayerPrefs.SetFloat(MasterKey, Master);
            PlayerPrefs.SetFloat(MusicKey, Music);
            PlayerPrefs.SetFloat(SfxKey, Sfx);
            PlayerPrefs.Save();
            Apply();
        }

        private static void Apply()
        {
            AudioListener.volume = Master;
            Changed?.Invoke();
        }
    }
}
