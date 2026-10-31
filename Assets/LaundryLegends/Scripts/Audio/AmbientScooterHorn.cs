using UnityEngine;

namespace LaundryLegends.Audio
{
    /// <summary>
    /// Rare street ambience: a short synthesized scooter horn every 3–5 minutes.
    /// Generated at runtime so no external audio asset or licence is involved.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbientScooterHorn : MonoBehaviour
    {
        [SerializeField] private float _minSeconds = 180f;
        [SerializeField] private float _maxSeconds = 300f;
        [SerializeField] private float _volume = 0.22f;

        private AudioSource _source;
        private float _nextPlayTime;

        public float MinSeconds => _minSeconds;

        public float MaxSeconds => _maxSeconds;

        public AudioSource Source => _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = _volume;
            _source.clip = BuildHornClip();
            ScheduleNext();
        }

        private void Update()
        {
            if (_source == null || Time.time < _nextPlayTime)
            {
                return;
            }

            _source.PlayOneShot(_source.clip, _volume);
            ScheduleNext();
        }

        private void ScheduleNext()
        {
            _nextPlayTime = Time.time + Random.Range(_minSeconds, _maxSeconds);
        }

        /// <summary>Two short tones, like a distant scooter horn.</summary>
        public static AudioClip BuildHornClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.42f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                bool secondTone = t > 0.21f;
                if (secondTone)
                {
                    t -= 0.21f;
                }

                float envelope = Mathf.Clamp01(1f - (t / 0.21f));
                float frequency = secondTone ? 620f : 470f;
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                data[i] = tone * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create("ScooterHorn", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
