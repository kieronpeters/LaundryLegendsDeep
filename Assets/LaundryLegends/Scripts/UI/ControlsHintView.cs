using LaundryLegends.Interaction;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.UI
{
    /// <summary>
    /// Always-visible IMGUI key legend. It reads the real bindings from the
    /// player components instead of hard-coding them, so a remap cannot leave the
    /// HUD lying. Sits directly under the order ticket.
    /// </summary>
    public sealed class ControlsHintView : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private CarryController _carry;
        [SerializeField] private float _left = 16f;
        [SerializeField] private float _top = 316f;
        [SerializeField] private float _width = 300f;
        [SerializeField] private float _height = 150f;

        public PlayerInteractor Interactor => _interactor;

        public CarryController Carry => _carry;

        /// <summary>Developer/test hook: relocates the panel without touching the scene.</summary>
        public void Configure(PlayerInteractor interactor, CarryController carry)
        {
            _interactor = interactor;
            _carry = carry;
        }

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Resolve();
        }

        /// <summary>Builds the key legend from the live bindings.</summary>
        public string BuildText()
        {
            string interact = _interactor != null ? _interactor.InteractKeyLabel : "E";
            string drop = _carry != null ? _carry.DropKeyLabel : "Q";
            return
                "WASD   Move\n" +
                "Mouse  Look\n" +
                $"{interact}      Pick up / interact\n" +
                $"{drop}      Drop carried item\n" +
                "U      Buy next upgrade\n" +
                "Esc    Pause";
        }

        private void OnGUI()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            GUI.Box(new Rect(_left, _top, _width, _height), "CONTROLS");
            GUI.Label(new Rect(_left + 12f, _top + 24f, _width - 24f, _height - 30f), BuildText());
        }

        private void Resolve()
        {
            if (_interactor == null)
            {
                _interactor = GetComponent<PlayerInteractor>();
            }

            if (_interactor == null)
            {
                _interactor = FindFirstObjectByType<PlayerInteractor>();
            }

            if (_carry == null)
            {
                _carry = GetComponent<CarryController>();
            }

            if (_carry == null)
            {
                _carry = FindFirstObjectByType<CarryController>();
            }
        }
    }
}
