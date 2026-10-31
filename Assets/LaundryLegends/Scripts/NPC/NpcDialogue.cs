using System;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Cycles short ambient dialogue lines when the player is nearby and exposes
    /// the active line so a subtitle view can render it. Lines come either from
    /// explicit text or from <see cref="DialogueBank"/> for a speaker archetype;
    /// text always renders even when no audio clip exists.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcDialogue : MonoBehaviour
    {
        [SerializeField] private string[] _lines = new string[0];
        [SerializeField] private float _interval = 6f;
        [SerializeField] private float _range = 3.5f;
        [SerializeField] private NpcArchetype _archetype = NpcArchetype.Attendant;
        [SerializeField] private DialogueTrigger _bankTrigger = DialogueTrigger.Idle;
        [SerializeField] private bool _useDialogueBank;
        [SerializeField] private bool _drawOwnSubtitle = true;
        [SerializeField] private bool _ambientEnabled = true;

        private readonly System.Random _random = new System.Random();
        private Transform _player;
        private int _index;
        private float _timer;

        /// <summary>
        /// Raised with this component and the line whenever a new line starts,
        /// whether it came from explicit text or from <see cref="DialogueBank"/>.
        /// Subscribers replace any line still on screen.
        /// </summary>
        public event Action<NpcDialogue, string> LineStarted;

        /// <summary>The line currently being spoken; empty when none is active.</summary>
        public string CurrentLine { get; private set; } = string.Empty;

        /// <summary>Whether a line should currently be rendered.</summary>
        public bool IsTalking { get; private set; }

        /// <summary>Number of explicitly configured lines (zero while using the bank).</summary>
        public int ExplicitLineCount => _lines != null ? _lines.Length : 0;

        /// <summary>Archetype whose bank pool this NPC draws from.</summary>
        public NpcArchetype Archetype => _archetype;

        /// <summary>Trigger used for the ambient bank line.</summary>
        public DialogueTrigger BankTrigger => _bankTrigger;

        /// <summary>Whether ambient lines come from <see cref="DialogueBank"/>.</summary>
        public bool IsUsingDialogueBank => _useDialogueBank;

        /// <summary>Whether this component still draws its own IMGUI fallback subtitle.</summary>
        public bool DrawsOwnSubtitle => _drawOwnSubtitle;

        /// <summary>Whether the built-in distance/timer ambient loop is active.</summary>
        public bool AmbientEnabled => _ambientEnabled;

        /// <summary>
        /// Sets explicit lines and leaves bank mode. Kept for the scene setup
        /// tool and existing scenes; call <see cref="ConfigureForBank"/> after
        /// this to switch an NPC over to the bank.
        /// </summary>
        /// <param name="lines">Lines to cycle in order.</param>
        public void Configure(params string[] lines)
        {
            _lines = lines ?? new string[0];
            _useDialogueBank = false;
            _index = 0;
            _timer = 0f;
            CurrentLine = string.Empty;
        }

        /// <summary>
        /// Switches ambient lines to the dialogue bank for this archetype and
        /// trigger. Explicit lines are cleared.
        /// </summary>
        /// <param name="archetype">Archetype whose pool supplies the lines.</param>
        /// <param name="trigger">Ambient trigger to pick from.</param>
        public void ConfigureForBank(NpcArchetype archetype, DialogueTrigger trigger = DialogueTrigger.Idle)
        {
            _archetype = archetype;
            _bankTrigger = trigger;
            _useDialogueBank = true;
            _lines = new string[0];
            _index = 0;
            _timer = 0f;
            CurrentLine = string.Empty;
        }

        /// <summary>
        /// Changes the ambient bank trigger, for example from <see cref="DialogueTrigger.Enter"/>
        /// to <see cref="DialogueTrigger.Counter"/>. The next line is due immediately.
        /// </summary>
        /// <param name="trigger">New ambient trigger.</param>
        public void SetBankTrigger(DialogueTrigger trigger)
        {
            _bankTrigger = trigger;
            _timer = 0f;
        }

        /// <summary>
        /// Picks a line for this NPC's archetype and the given trigger, then
        /// speaks it. Lets other systems request a specific moment without
        /// knowing which line was authored for it.
        /// </summary>
        /// <param name="trigger">Conversation moment to pick from.</param>
        /// <returns>The line that started, or <c>null</c> when the pool is empty.</returns>
        public string SpeakFromBank(DialogueTrigger trigger)
        {
            string line = DialogueBank.Pick(_archetype, trigger, _random);
            if (line == null)
            {
                return null;
            }

            Say(line);
            return line;
        }

        /// <summary>
        /// Starts a line immediately and raises <see cref="LineStarted"/>. Used
        /// by systems that already decided what the NPC should say.
        /// </summary>
        /// <param name="line">Line to speak; empty or null is ignored.</param>
        public void Say(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            CurrentLine = line;
            IsTalking = true;
            _timer = _interval;
            LineStarted?.Invoke(this, line);
        }

        /// <summary>
        /// Enables or disables the IMGUI fallback subtitle. A subtitle view
        /// disables it while bound so the two never draw at the same time.
        /// </summary>
        /// <param name="draw">Whether this component draws its own box.</param>
        public void SetOwnSubtitleEnabled(bool draw)
        {
            _drawOwnSubtitle = draw;
        }

        /// <summary>
        /// Enables or disables the built-in distance/timer ambient loop. A
        /// <see cref="DialogueVoicePlayer"/> disables it and calls
        /// <see cref="Say"/> directly so voice, text, and the global cooldown
        /// stay in sync.
        /// </summary>
        /// <param name="isEnabled">Whether the ambient loop may pick lines on its own.</param>
        public void SetAmbientEnabled(bool isEnabled)
        {
            _ambientEnabled = isEnabled;
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _player = player != null ? player.transform : null;
        }

        private void Update()
        {
            IsTalking = false;
            if (!_ambientEnabled || _player == null || Time.timeScale <= 0f)
            {
                return;
            }

            if (Vector3.Distance(transform.position, _player.position) > _range)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                string next = NextLine();
                if (!string.IsNullOrEmpty(next))
                {
                    Say(next);
                }
                else
                {
                    _timer = _interval;
                }
            }

            IsTalking = !string.IsNullOrEmpty(CurrentLine);
        }

        private string NextLine()
        {
            if (_useDialogueBank)
            {
                string bankLine = DialogueBank.Pick(_archetype, _bankTrigger, _random);
                if (bankLine != null)
                {
                    return bankLine;
                }
            }

            if (_lines == null || _lines.Length == 0)
            {
                return null;
            }

            string line = _lines[_index % _lines.Length];
            _index++;
            return line;
        }

        private void OnGUI()
        {
            if (!_drawOwnSubtitle || !IsTalking || string.IsNullOrEmpty(CurrentLine))
            {
                return;
            }

            const float width = 520f;
            const float height = 48f;
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 150f, width, height);
            GUI.Box(rect, CurrentLine);
        }
    }
}
