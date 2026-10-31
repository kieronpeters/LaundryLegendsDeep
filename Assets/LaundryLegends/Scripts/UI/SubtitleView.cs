using System.Collections.Generic;
using LaundryLegends.NPC;
using UnityEngine;

namespace LaundryLegends.UI
{
    /// <summary>
    /// Subtitle-first dialogue display. Binds to <see cref="NpcDialogue"/>
    /// speakers, renders the active line, and hides it after a text-based read
    /// window. An audio clip is optional: a line with no clip still appears for
    /// the same readable duration, so voice remains a later pass. Every timing
    /// constant lives here. It never changes cursor lock or pause state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SubtitleView : MonoBehaviour
    {
        /// <summary>Average reading rate used to estimate a text-only line duration.</summary>
        public const float CharactersPerSecond = 14f;

        /// <summary>Shortest time any subtitle stays on screen.</summary>
        public const float MinimumSeconds = 2.5f;

        /// <summary>Longest time a single subtitle stays on screen.</summary>
        public const float MaximumSeconds = 8f;

        /// <summary>Extra hold time so the last word is not cut off.</summary>
        public const float TailSeconds = 0.4f;

        [SerializeField] private NpcDialogue _speaker;
        [SerializeField] private AudioSource _voice;
        [SerializeField] private bool _autoBindSceneSpeakers = true;

        private readonly List<NpcDialogue> _bound = new List<NpcDialogue>();
        private float _remaining;

        /// <summary>Text currently rendered; empty while hidden.</summary>
        public string CurrentSubtitle { get; private set; } = string.Empty;

        /// <summary>Whether a subtitle is currently on screen.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Seconds left before the current subtitle hides.</summary>
        public float TimeRemaining => _remaining;

        /// <summary>The explicitly assigned speaker, or null when the view binds the scene.</summary>
        public NpcDialogue Speaker => _speaker;

        /// <summary>
        /// Estimates how long a text-only line should stay on screen from its
        /// length, clamped so short lines remain readable and long lines never
        /// overstay. Empty input still returns <see cref="MinimumSeconds"/>.
        /// </summary>
        /// <param name="line">Line to estimate.</param>
        /// <returns>Duration in seconds.</returns>
        public static float EstimateDuration(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return MinimumSeconds;
            }

            float estimate = (line.Length / CharactersPerSecond) + TailSeconds;
            return Mathf.Clamp(estimate, MinimumSeconds, MaximumSeconds);
        }

        /// <summary>
        /// Shows a text-only line. The subtitle always appears, which is the
        /// permanent fallback when no voice clip exists.
        /// </summary>
        /// <param name="line">Line to render; empty hides the subtitle.</param>
        public void Show(string line)
        {
            Show(line, null);
        }

        /// <summary>
        /// Shows a line and, when both a clip and a voice source exist, plays
        /// the clip once. Without a clip the subtitle uses the text estimate.
        /// </summary>
        /// <param name="line">Line to render; empty hides the subtitle.</param>
        /// <param name="clip">Optional clip for this line.</param>
        public void Show(string line, AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                Hide();
                return;
            }

            CurrentSubtitle = line;
            IsVisible = true;
            _remaining = clip != null
                ? Mathf.Max(clip.length + TailSeconds, MinimumSeconds)
                : EstimateDuration(line);

            if (clip != null && _voice != null)
            {
                _voice.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Wires the view to one speaker and voice source. Passing a speaker
        /// disables scene-wide auto-binding, so each NPC's view only renders
        /// that NPC's lines. Binding itself still happens in <see cref="OnEnable"/>.
        /// </summary>
        /// <param name="speaker">Speaker whose lines this view renders.</param>
        /// <param name="voice">Optional audio source for clip-driven lines.</param>
        /// <param name="autoBindSceneSpeakers">Whether to bind every speaker in the scene.</param>
        public void Configure(NpcDialogue speaker, AudioSource voice, bool autoBindSceneSpeakers = false)
        {
            _speaker = speaker;
            _voice = voice;
            _autoBindSceneSpeakers = autoBindSceneSpeakers;
        }

        /// <summary>Hides the subtitle immediately.</summary>
        public void Hide()
        {
            CurrentSubtitle = string.Empty;
            IsVisible = false;
            _remaining = 0f;
        }

        /// <summary>
        /// Binds a speaker so its lines render here, and turns off that
        /// speaker's own IMGUI fallback to avoid drawing twice.
        /// </summary>
        /// <param name="speaker">Speaker to listen to.</param>
        public void Bind(NpcDialogue speaker)
        {
            if (speaker == null || _bound.Contains(speaker))
            {
                return;
            }

            _bound.Add(speaker);
            speaker.LineStarted += OnLineStarted;
            speaker.SetOwnSubtitleEnabled(false);
        }

        /// <summary>
        /// Stops listening to a speaker and restores its fallback subtitle.
        /// </summary>
        /// <param name="speaker">Speaker to release.</param>
        public void Unbind(NpcDialogue speaker)
        {
            if (speaker == null || !_bound.Remove(speaker))
            {
                return;
            }

            speaker.LineStarted -= OnLineStarted;
            speaker.SetOwnSubtitleEnabled(true);
        }

        private void OnEnable()
        {
            if (_speaker != null)
            {
                Bind(_speaker);
                return;
            }

            if (!_autoBindSceneSpeakers)
            {
                return;
            }

            NpcDialogue[] speakers = FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None);
            for (int i = 0; i < speakers.Length; i++)
            {
                Bind(speakers[i]);
            }
        }

        private void OnDisable()
        {
            for (int i = _bound.Count - 1; i >= 0; i--)
            {
                NpcDialogue speaker = _bound[i];
                _bound.RemoveAt(i);
                if (speaker == null)
                {
                    continue;
                }

                speaker.LineStarted -= OnLineStarted;
                speaker.SetOwnSubtitleEnabled(true);
            }

            Hide();
        }

        private void Update()
        {
            if (!IsVisible || Time.timeScale <= 0f)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Hide();
            }
        }

        private void OnLineStarted(NpcDialogue speaker, string line)
        {
            Show(line);
        }

        private void OnGUI()
        {
            if (!IsVisible || string.IsNullOrEmpty(CurrentSubtitle))
            {
                return;
            }

            const float width = 520f;
            const float height = 48f;
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 150f, width, height);
            GUI.Box(rect, CurrentSubtitle);
        }
    }
}
