using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Cycles short ambient dialogue lines when the player is nearby and draws
    /// a simple IMGUI subtitle. Text-only placeholder until the dialogue UI phase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcDialogue : MonoBehaviour
    {
        [SerializeField] private string[] _lines = new string[0];
        [SerializeField] private float _interval = 6f;
        [SerializeField] private float _range = 3.5f;

        private Transform _player;
        private int _index;
        private float _timer;

        public string CurrentLine { get; private set; } = string.Empty;
        public bool IsTalking { get; private set; }

        public void Configure(params string[] lines)
        {
            _lines = lines ?? new string[0];
            _index = 0;
            _timer = 0f;
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _player = player != null ? player.transform : null;
        }

        private void Update()
        {
            IsTalking = false;
            if (_player == null || _lines.Length == 0 || Time.timeScale <= 0f)
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
                CurrentLine = _lines[_index % _lines.Length];
                _index++;
                _timer = _interval;
            }

            IsTalking = !string.IsNullOrEmpty(CurrentLine);
        }

        private void OnGUI()
        {
            if (!IsTalking)
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
