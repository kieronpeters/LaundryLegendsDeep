using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Audio
{
    /// <summary>Short interface sounds generated in code so no external SFX pack is required.</summary>
    public enum Sfx
    {
        Click,
        Pickup,
        Place,
        MachineStart,
        MachineDone,
        Wrap,
        Cash,
        Error,
        Bell,
        Coin
    }

    /// <summary>
    /// Builds and caches small procedural AudioClips. The project has no verified
    /// SFX-generation tool, so these are intentionally simple synthesized cues.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 22050;
        private static readonly Dictionary<Sfx, AudioClip> Cache = new Dictionary<Sfx, AudioClip>();
        private static readonly Dictionary<bool, AudioClip> AmbienceCache = new Dictionary<bool, AudioClip>();

        public static AudioClip Get(Sfx sfx)
        {
            if (Cache.TryGetValue(sfx, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip clip = Build(sfx);
            Cache[sfx] = clip;
            return clip;
        }

        /// <summary>Long looping bed; true switches to the arcade variant.</summary>
        public static AudioClip GetAmbience(bool arcade)
        {
            if (AmbienceCache.TryGetValue(arcade, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip clip = BuildAmbience(arcade);
            AmbienceCache[arcade] = clip;
            return clip;
        }

        private static AudioClip Build(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Click:
                    return Tone("SfxClick", 0.06f, 1200f, 1200f, 0.35f, false);
                case Sfx.Pickup:
                    return Tone("SfxPickup", 0.14f, 420f, 880f, 0.4f, false);
                case Sfx.Place:
                    return Tone("SfxPlace", 0.14f, 760f, 380f, 0.4f, false);
                case Sfx.MachineStart:
                    return Tone("SfxMachineStart", 0.4f, 90f, 180f, 0.5f, true);
                case Sfx.MachineDone:
                    return Tone("SfxMachineDone", 0.45f, 660f, 990f, 0.4f, false);
                case Sfx.Wrap:
                    return Noise("SfxWrap", 0.18f, 0.35f);
                case Sfx.Cash:
                    return Tone("SfxCash", 0.3f, 880f, 1320f, 0.35f, false);
                case Sfx.Error:
                    return Tone("SfxError", 0.22f, 220f, 160f, 0.5f, true);
                case Sfx.Bell:
                    return Tone("SfxBell", 0.55f, 1046f, 1568f, 0.3f, false);
                case Sfx.Coin:
                    return Tone("SfxCoin", 0.18f, 1500f, 2000f, 0.35f, false);
                default:
                    return Tone("SfxClick", 0.06f, 1200f, 1200f, 0.35f, false);
            }
        }

        private static AudioClip Tone(
            string name,
            float duration,
            float startHz,
            float endHz,
            float volume,
            bool noise)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            var random = new System.Random(name.Length * 31 + samples);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)samples;
                float frequency = Mathf.Lerp(startHz, endHz, progress);
                float envelope = Mathf.Sin(Mathf.PI * progress);
                float value = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
                if (noise)
                {
                    value += ((float)random.NextDouble() * 2f - 1f) * 0.05f * envelope;
                }

                data[i] = Mathf.Clamp(value, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Noise(string name, float duration, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            var random = new System.Random(name.Length * 17 + samples);
            for (int i = 0; i < samples; i++)
            {
                float progress = i / (float)samples;
                float envelope = Mathf.Sin(Mathf.PI * progress);
                data[i] = ((float)random.NextDouble() * 2f - 1f) * volume * envelope;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildAmbience(bool arcade)
        {
            const float duration = 4f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            var data = new float[samples];
            var random = new System.Random(arcade ? 7 : 3);
            float low = arcade ? 55f : 70f;
            float high = arcade ? 110f : 140f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float drone = Mathf.Sin(2f * Mathf.PI * low * t) * 0.05f
                              + Mathf.Sin(2f * Mathf.PI * high * t) * 0.02f;
                float hiss = ((float)random.NextDouble() * 2f - 1f) * 0.012f;
                float pulse = arcade
                    ? Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 0.015f
                    : 0f;
                data[i] = Mathf.Clamp(drone + hiss + pulse, -0.2f, 0.2f);
            }

            AudioClip clip = AudioClip.Create(
                arcade ? "AmbienceArcade" : "AmbienceLaundry",
                samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
