using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Plays voiced dialogue for one NPC. Each speak request picks an ordered
    /// clip from <see cref="DialogueVoiceLibrary"/>, raises the matching
    /// <see cref="DialogueBank"/> line through <see cref="NpcDialogue"/> so a
    /// subtitle view can render it, and reserves one global cooldown so two
    /// NPCs never talk over each other. When a key has no clips the line still
    /// appears as a subtitle.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueVoicePlayer : MonoBehaviour
    {
        /// <summary>Default gap between any two spoken lines in the scene.</summary>
        public const float DefaultGlobalCooldownSeconds = 8f;

        /// <summary>Default delay between ambient lines from one NPC.</summary>
        public const float DefaultIntervalSeconds = 12f;

        /// <summary>Default distance at which ambient speech is considered in range.</summary>
        public const float DefaultRange = 4.5f;

        private static float s_globalCooldownUntil;

        [SerializeField] private NpcArchetype _archetype = NpcArchetype.Attendant;
        [SerializeField] private DialogueVoiceLibrary _library;
        [SerializeField] private AudioSource _source;
        [SerializeField] private NpcDialogue _speaker;
        [SerializeField] private DialogueTrigger[] _triggers =
        {
            DialogueTrigger.Idle,
            DialogueTrigger.Counter
        };
        [SerializeField] private float _interval = DefaultIntervalSeconds;
        [SerializeField] private float _range = DefaultRange;
        [SerializeField] private float _initialDelay = 1.5f;
        [SerializeField] private float _globalCooldownSeconds = DefaultGlobalCooldownSeconds;
        [SerializeField] private bool _autoSpeak = true;

        private readonly Dictionary<DialogueTrigger, List<int>> _decks =
            new Dictionary<DialogueTrigger, List<int>>();
        private readonly Dictionary<DialogueTrigger, int> _deckSizes =
            new Dictionary<DialogueTrigger, int>();
        private readonly System.Random _random = new System.Random();

        private Transform _player;
        private float _timer;
        private int _triggerIndex;
        private bool _warnedMissingSpeaker;

        /// <summary>Archetype whose line pool this NPC speaks.</summary>
        public NpcArchetype Archetype => _archetype;

        /// <summary>Voice library asset used for clip lookups.</summary>
        public DialogueVoiceLibrary Library => _library;

        /// <summary>Audio source used for one-shot playback.</summary>
        public AudioSource Source => _source;

        /// <summary>Speaker that raises the line text for subtitles.</summary>
        public NpcDialogue Speaker => _speaker;

        /// <summary>Whether this component drives its own ambient speech loop.</summary>
        public bool AutoSpeak => _autoSpeak;

        /// <summary>Triggers cycled by the ambient speech loop.</summary>
        public DialogueTrigger[] Triggers => _triggers;

        /// <summary>Line raised by the most recent successful speak.</summary>
        public string LastLine { get; private set; } = string.Empty;

        /// <summary>Clip played by the most recent successful speak; null for text-only.</summary>
        public AudioClip LastClip { get; private set; }

        /// <summary>Trigger used by the most recent successful speak.</summary>
        public DialogueTrigger LastTrigger { get; private set; }

        /// <summary>Whether any NPC currently owns the global speech cooldown.</summary>
        public static bool IsGlobalCooldownActive => Time.unscaledTime < s_globalCooldownUntil;

        /// <summary>Seconds left in the global speech cooldown.</summary>
        public static float GlobalCooldownRemaining =>
            Mathf.Max(0f, s_globalCooldownUntil - Time.unscaledTime);

        /// <summary>Clears the global speech cooldown. Intended for tests and capture tools.</summary>
        public static void ResetGlobalCooldown()
        {
            s_globalCooldownUntil = 0f;
        }

        /// <summary>
        /// Wires every dependency explicitly. Editor tooling and tests both use
        /// this instead of relying on scene searches.
        /// </summary>
        /// <param name="archetype">Archetype whose dialogue bank lines to speak.</param>
        /// <param name="library">Voice library to look clips up in.</param>
        /// <param name="source">Audio source for one-shot playback.</param>
        /// <param name="speaker">Speaker that raises the subtitle line.</param>
        /// <param name="triggers">Ambient trigger order; may be empty to disable ambient speech.</param>
        /// <param name="interval">Seconds between ambient lines.</param>
        /// <param name="range">Player distance at which ambient lines may fire.</param>
        /// <param name="autoSpeak">Whether the ambient loop runs.</param>
        public void Configure(
            NpcArchetype archetype,
            DialogueVoiceLibrary library,
            AudioSource source,
            NpcDialogue speaker,
            DialogueTrigger[] triggers,
            float interval = DefaultIntervalSeconds,
            float range = DefaultRange,
            bool autoSpeak = true)
        {
            _archetype = archetype;
            _library = library;
            _source = source;
            _speaker = speaker;
            _triggers = triggers ?? Array.Empty<DialogueTrigger>();
            _interval = Mathf.Max(1f, interval);
            _range = Mathf.Max(0.5f, range);
            _autoSpeak = autoSpeak;
            _timer = Mathf.Max(0f, _initialDelay);
            _triggerIndex = 0;
            _decks.Clear();
            _deckSizes.Clear();

            if (_speaker != null)
            {
                _speaker.SetAmbientEnabled(false);
            }
        }

        /// <summary>
        /// Speaks one line for a trigger. Returns false when the global cooldown
        /// is active or the key has no text; a missing clip is not an error and
        /// still produces a subtitle.
        /// </summary>
        /// <param name="trigger">Conversation moment to speak.</param>
        /// <returns>True when a line was raised.</returns>
        public bool Speak(DialogueTrigger trigger)
        {
            if (IsGlobalCooldownActive)
            {
                return false;
            }

            if (_speaker == null)
            {
                _speaker = GetComponent<NpcDialogue>();
            }

            if (_speaker == null)
            {
                if (!_warnedMissingSpeaker)
                {
                    _warnedMissingSpeaker = true;
                    Debug.LogWarning($"[Voice] {name} has no NpcDialogue; voice line cannot raise a subtitle.");
                }

                return false;
            }

            IReadOnlyList<AudioClip> clips = _library != null
                ? _library.GetClips(_archetype, trigger)
                : Array.Empty<AudioClip>();

            AudioClip clip = null;
            string line;
            if (clips.Count > 0)
            {
                int clipIndex = DrawClipIndex(trigger, clips.Count);
                clip = clips[clipIndex];
                line = ResolveLine(trigger, clipIndex);
            }
            else
            {
                // No voice for this key: keep the subtitle-only path alive.
                line = DialogueBank.Pick(_archetype, trigger, _random);
            }

            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            _speaker.Say(line);

            if (clip != null && _source != null)
            {
                _source.PlayOneShot(clip);
            }

            LastLine = line;
            LastClip = clip;
            LastTrigger = trigger;
            s_globalCooldownUntil = Time.unscaledTime + Mathf.Max(0f, _globalCooldownSeconds);
            return true;
        }

        private void Awake()
        {
            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
            }

            if (_speaker == null)
            {
                _speaker = GetComponent<NpcDialogue>();
            }
        }

        private void OnEnable()
        {
            _timer = Mathf.Max(0f, _initialDelay);
            if (_speaker != null)
            {
                _speaker.SetAmbientEnabled(false);
            }
        }

        private void Start()
        {
            ResolvePlayer();
        }

        private void Update()
        {
            if (!_autoSpeak || _triggers == null || _triggers.Length == 0)
            {
                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (_player == null)
            {
                ResolvePlayer();
                if (_player == null)
                {
                    return;
                }
            }

            if (Vector3.Distance(transform.position, _player.position) > _range)
            {
                return;
            }

            if (IsGlobalCooldownActive)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            DialogueTrigger trigger = _triggers[_triggerIndex % _triggers.Length];
            _triggerIndex++;
            _timer = Speak(trigger) ? _interval : 0.5f;
        }

        private string ResolveLine(DialogueTrigger trigger, int clipIndex)
        {
            IReadOnlyList<string> lines = DialogueBank.GetLines(_archetype, trigger);
            if (clipIndex >= 0 && clipIndex < lines.Count)
            {
                return lines[clipIndex];
            }

            if (lines.Count > 0)
            {
                return lines[_random.Next(lines.Count)];
            }

            return null;
        }

        /// <summary>
        /// Draws from a per-trigger shuffled deck so a clip cannot repeat until
        /// every clip for that key has been used once.
        /// </summary>
        private int DrawClipIndex(DialogueTrigger trigger, int count)
        {
            if (!_decks.TryGetValue(trigger, out List<int> deck)
                || !_deckSizes.TryGetValue(trigger, out int size)
                || size != count
                || deck.Count == 0)
            {
                deck = new List<int>(count);
                for (int i = 0; i < count; i++)
                {
                    deck.Add(i);
                }

                Shuffle(deck);
                _decks[trigger] = deck;
                _deckSizes[trigger] = count;
            }

            int last = deck.Count - 1;
            int pick = deck[last];
            deck.RemoveAt(last);
            return pick;
        }

        private void Shuffle(List<int> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int swap = _random.Next(i + 1);
                int value = deck[i];
                deck[i] = deck[swap];
                deck[swap] = value;
            }
        }

        private void ResolvePlayer()
        {
            if (_player != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _player = player != null ? player.transform : null;
        }
    }
}
