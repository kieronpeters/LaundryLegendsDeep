using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Visible and audible attract mode for an installed, active cabinet.
    /// <see cref="ArcadeService"/> drives <see cref="SetPowered"/> and
    /// <see cref="SetBusy"/>, so <see cref="IsAttracting"/> can only be true
    /// while the machine is active and the player is not playing.
    /// <para>
    /// The pulse writes the screen material's emission colour. It uses the
    /// material instance created by <see cref="MazeGameView"/>, so neither the
    /// shared material asset nor any other renderer is changed.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeAttractMode : MonoBehaviour
    {
        private const string EmissionKeyword = "_EMISSION";
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer _screen;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _cue;
        [SerializeField] private Color _emissionColor = new Color(0.35f, 0.8f, 1f);
        [SerializeField, Min(0f)] private float _minIntensity = 0.15f;
        [SerializeField, Min(0f)] private float _maxIntensity = 1.2f;
        [SerializeField, Min(0.01f)] private float _pulseHz = 0.6f;
        [SerializeField] private bool _playCue = true;

        private Material _material;
        private AudioClip _generatedCue;
        private bool _powered;
        private bool _busy;

        /// <summary>True only while the machine is active, idle, and pulsing.</summary>
        public bool IsAttracting { get; private set; }

        /// <summary>Current pulse intensity; 0 when not attracting.</summary>
        public float Intensity01 { get; private set; }

        public bool IsPowered => _powered;

        public bool IsBusy => _busy;

        public Renderer Screen => _screen;

        public AudioSource Audio => _audio;

        /// <summary>Authoring/test wiring for the screen and cue.</summary>
        public void Configure(Renderer screen, AudioSource audio, AudioClip cue = null)
        {
            _screen = screen;
            _audio = audio;
            _cue = cue;
            _material = null;
        }

        /// <summary>Called by <see cref="ArcadeService"/> once the phase reaches Active.</summary>
        public void SetPowered(bool powered)
        {
            _powered = powered;
            Refresh();
        }

        /// <summary>Called by <see cref="ArcadeService"/> while the player is playing.</summary>
        public void SetBusy(bool busy)
        {
            _busy = busy;
            Refresh();
        }

        /// <summary>
        /// Applies one pulse sample. Public so tests do not depend on a frame
        /// clock; <see cref="Update"/> feeds it <see cref="Time.time"/>.
        /// </summary>
        public void Step(float timeSeconds)
        {
            if (!IsAttracting)
            {
                Intensity01 = 0f;
                return;
            }

            float wave = (Mathf.Sin(timeSeconds * Mathf.PI * 2f * _pulseHz) + 1f) * 0.5f;
            Intensity01 = Mathf.Lerp(_minIntensity, _maxIntensity, wave);
            SetEmission(_emissionColor * Intensity01);
        }

        private void Update()
        {
            if (!IsAttracting)
            {
                return;
            }

            Step(Time.time);
        }

        private void OnDisable()
        {
            if (!IsAttracting)
            {
                return;
            }

            IsAttracting = false;
            Intensity01 = 0f;
            SetEmission(Color.black);
            StopCue();
        }

        private void OnDestroy()
        {
            // The screen material is a per-instance copy created by the first
            // .material access; release it so repeated enabling does not leak.
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        private void Refresh()
        {
            bool shouldAttract = _powered && !_busy;
            if (shouldAttract == IsAttracting)
            {
                return;
            }

            IsAttracting = shouldAttract;
            if (shouldAttract)
            {
                Intensity01 = _minIntensity;
                SetEmission(_emissionColor * _minIntensity);
                StartCue();
            }
            else
            {
                Intensity01 = 0f;
                SetEmission(Color.black);
                StopCue();
            }
        }

        private void StartCue()
        {
            if (!_playCue || _audio == null)
            {
                return;
            }

            if (_audio.clip == null)
            {
                _audio.clip = _cue != null ? _cue : GeneratedCue();
            }

            _audio.loop = true;
            _audio.Play();
        }

        private void StopCue()
        {
            if (_audio != null && _audio.isPlaying)
            {
                _audio.Stop();
            }
        }

        private void SetEmission(Color color)
        {
            Material material = ScreenMaterial();
            if (material == null)
            {
                return;
            }

            material.EnableKeyword(EmissionKeyword);
            material.SetColor(EmissionColorId, color);
        }

        private Material ScreenMaterial()
        {
            if (_material != null)
            {
                return _material;
            }

            if (_screen == null || !Application.isPlaying)
            {
                return null;
            }

            _material = _screen.material;
            return _material;
        }

        /// <summary>
        /// Placeholder attract cue: a soft two-tone loop built in code so the
        /// arcade can be audible without adding an audio asset.
        /// </summary>
        private AudioClip GeneratedCue()
        {
            if (_generatedCue != null)
            {
                return _generatedCue;
            }

            const int sampleRate = 22050;
            const float duration = 1f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.12f
                              + Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.06f) * envelope;
            }

            _generatedCue = AudioClip.Create("ArcadeAttractCue", sampleCount, 1, sampleRate, false);
            _generatedCue.SetData(samples, 0);
            return _generatedCue;
        }
    }
}
