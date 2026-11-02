using LaundryLegends.Arcade;
using UnityEngine;

namespace LaundryLegends.Audio
{
    /// <summary>
    /// Owns the one-shot SFX source and the laundry/arcade ambience beds. Created
    /// on first use so every scene gets audio without a manual bootstrap, and kept
    /// across scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameAudio : MonoBehaviour
    {
        private static GameAudio _instance;

        [SerializeField] private AudioSource _sfx;
        [SerializeField] private AudioSource _ambience;
        [SerializeField] private AudioSource _arcade;

        public AudioSource SfxSource => _sfx;
        public AudioSource AmbienceSource => _ambience;
        public AudioSource ArcadeSource => _arcade;

        public static bool Exists => _instance != null;

        public static GameAudio Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = new GameObject("GameAudio");
                    _instance = host.AddComponent<GameAudio>();
                }

                return _instance;
            }
        }

        /// <summary>Plays a one-shot cue; safe in edit mode and headless tests.</summary>
        public static void Play(Sfx sfx, float volume = 1f)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Instance.PlayInternal(sfx, volume);
        }

        /// <summary>Forces the arcade bed on/off; used by tests and cutscenes.</summary>
        public static void SetArcade(bool arcade)
        {
            if (_instance != null)
            {
                _instance._arcadeOverride = arcade;
                _instance._hasArcadeOverride = true;
            }
        }

        private bool _arcadeOverride;
        private bool _hasArcadeOverride;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSources();
        }

        private void BuildSources()
        {
            _sfx = CreateSource("Sfx", false, 0f);
            _ambience = CreateSource("LaundryAmbience", true, 0f);
            _arcade = CreateSource("ArcadeAmbience", true, 0f);
            _ambience.clip = ProceduralSfx.GetAmbience(false);
            _arcade.clip = ProceduralSfx.GetAmbience(true);
            _ambience.Play();
            _arcade.Play();
            _arcade.volume = 0f;
        }

        private AudioSource CreateSource(string sourceName, bool loop, float spatialBlend)
        {
            var host = new GameObject(sourceName);
            host.transform.SetParent(transform, false);
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = spatialBlend;
            source.volume = 0f;
            return source;
        }

        private void Update()
        {
            bool arcade = _hasArcadeOverride ? _arcadeOverride : ArcadeMode.IsActive;
            float target = arcade ? 1f : 0f;
            float blend = 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime);
            _ambience.volume = Mathf.Lerp(_ambience.volume, (1f - target) * 0.22f * GameAudioSettings.Music, blend);
            _arcade.volume = Mathf.Lerp(_arcade.volume, target * 0.22f * GameAudioSettings.Music, blend);
        }

        private void PlayInternal(Sfx sfx, float volume)
        {
            if (_sfx == null)
            {
                BuildSources();
            }

            AudioClip clip = ProceduralSfx.Get(sfx);
            if (clip != null)
            {
                _sfx.PlayOneShot(clip, Mathf.Clamp01(GameAudioSettings.Sfx * volume));
            }
        }
    }
}
