using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Always-visible IMGUI arcade panel: machines, income, reputation and the next
    /// milestone. Hidden while the arcade session owns the screen, during pause,
    /// and until the back room is usable.
    /// </summary>
    public sealed class ArcadePanelView : MonoBehaviour
    {
        [SerializeField] private ArcadeService _service;
        [SerializeField] private LaundryReputation _reputation;
        [SerializeField] private float _top = 16f;
        [SerializeField] private float _width = 340f;
        [SerializeField] private float _height = 118f;

        public ArcadeService Service => _service;

        public LaundryReputation Reputation => _reputation;

        public void Configure(ArcadeService service, LaundryReputation reputation)
        {
            _service = service;
            _reputation = reputation;
        }

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Resolve();
        }

        /// <summary>Builds the panel text from live state, for tests and the HUD.</summary>
        public string BuildText()
        {
            if (_service == null)
            {
                return "ARCADE\n(not wired)";
            }

            int machines = _service.ActiveMachineCount;
            int capacity = _service.Capacity;
            int reputation = _reputation != null ? _reputation.Value : 0;
            float footfall = _reputation != null ? _reputation.FootfallMultiplier : 1f;
            string next = _service.NextMilestone;
            string nextText = string.IsNullOrEmpty(next)
                ? "All milestones earned"
                : ArcadeMilestones.Describe(next);

            return
                $"ARCADE   machines {machines}/{capacity}\n" +
                $"Lifetime income ${_service.LifetimeIncome}   visits {_service.TotalVisits}\n" +
                $"Reputation {reputation}/100   footfall x{footfall:0.00}\n" +
                $"Next: {nextText}";
        }

        private void OnGUI()
        {
            if (_service == null || ArcadeMode.IsActive || Time.timeScale == 0f)
            {
                return;
            }

            if (!_service.IsRoomReady)
            {
                return;
            }

            float left = Mathf.Max(8f, Screen.width - _width - 16f);
            GUI.Box(new Rect(left, _top, _width, _height), "ARCADE BUSINESS");
            GUI.Label(new Rect(left + 12f, _top + 24f, _width - 24f, _height - 30f), BuildText());
        }

        private void Resolve()
        {
            if (_service == null)
            {
                _service = GetComponent<ArcadeService>();
            }

            if (_service == null)
            {
                _service = FindFirstObjectByType<ArcadeService>();
            }

            if (_reputation == null)
            {
                _reputation = GetComponent<LaundryReputation>();
            }

            if (_reputation == null)
            {
                _reputation = FindFirstObjectByType<LaundryReputation>();
            }

            if (_reputation == null && _service != null)
            {
                _reputation = _service.Reputation;
            }
        }
    }
}
