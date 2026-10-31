using LaundryLegends.Interaction;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.UI
{
    /// <summary>
    /// Temporary IMGUI interaction hint. It renders from explicit, testable
    /// state and reads the prompt and key label from <see cref="PlayerInteractor"/>
    /// so the input mapping has one source of truth. It never changes cursor
    /// lock or pause state. The UI phase replaces this placeholder.
    /// </summary>
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private CarryController _carry;

        /// <summary>Whether the prompt should currently be drawn.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Text rendered while visible; empty while hidden.</summary>
        public string CurrentLabel { get; private set; } = string.Empty;

        /// <summary>Whether the drop hint should currently be drawn.</summary>
        public bool IsDropHintVisible { get; private set; }

        /// <summary>Drop hint text; empty while nothing is held.</summary>
        public string DropHint { get; private set; } = string.Empty;

        /// <summary>The interactor supplying prompt, target, and key label.</summary>
        public PlayerInteractor Interactor => _interactor;

        /// <summary>The carry controller supplying held state and drop key.</summary>
        public CarryController Carry => _carry;

        private void Awake()
        {
            if (_interactor == null)
            {
                _interactor = GetComponent<PlayerInteractor>();
            }

            if (_carry == null)
            {
                _carry = GetComponent<CarryController>();
            }
        }

        private void OnEnable()
        {
            if (_interactor == null)
            {
                _interactor = GetComponent<PlayerInteractor>();
            }

            if (_carry == null)
            {
                _carry = GetComponent<CarryController>();
            }

            if (_interactor != null)
            {
                _interactor.TargetChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_interactor != null)
            {
                _interactor.TargetChanged -= Refresh;
            }

            Hide();
            IsDropHintVisible = false;
            DropHint = string.Empty;
        }

        private void Update()
        {
            // Also catches pause, eligibility, and destroyed-target changes that
            // do not raise TargetChanged.
            Refresh();
        }

        /// <summary>
        /// Recomputes visibility and label from the interactor. Public so tests
        /// and future input layers can force a refresh without waiting a frame.
        /// </summary>
        public void Refresh()
        {
            bool showDropHint = _carry != null && _carry.IsCarrying && Time.timeScale > 0f;
            IsDropHintVisible = showDropHint;
            DropHint = showDropHint ? $"[{_carry.DropKeyLabel}] Drop" : string.Empty;

            if (_interactor == null || Time.timeScale <= 0f)
            {
                Hide();
                return;
            }

            IInteractable interactable = _interactor.CurrentInteractable;
            if (!IsAlive(interactable) || !interactable.CanInteract(_interactor.gameObject))
            {
                Hide();
                return;
            }

            string prompt = _interactor.CurrentPrompt;
            if (string.IsNullOrEmpty(prompt))
            {
                prompt = interactable.InteractionPrompt;
            }

            string keyLabel = _interactor.InteractKeyLabel;
            IsVisible = true;
            CurrentLabel = string.IsNullOrEmpty(keyLabel) ? prompt : $"{prompt} [{keyLabel}]";
        }

        private void Hide()
        {
            IsVisible = false;
            CurrentLabel = string.Empty;
        }
        private void OnGUI()
        {
            if (IsVisible && !string.IsNullOrEmpty(CurrentLabel))
            {
                const float width = 360f;
                const float height = 34f;
                var rect = new Rect(
                    (Screen.width - width) * 0.5f,
                    Screen.height - 96f,
                    width,
                    height);
                GUI.Box(rect, CurrentLabel);
            }

            if (IsDropHintVisible && !string.IsNullOrEmpty(DropHint))
            {
                const float width = 140f;
                const float height = 28f;
                var rect = new Rect(
                    (Screen.width - width) * 0.5f,
                    Screen.height - 54f,
                    width,
                    height);
                GUI.Box(rect, DropHint);
            }
        }

        /// <summary>
        /// Treats a destroyed Unity object as dead while allowing non-Unity
        /// implementations of the interface.
        /// </summary>
        private static bool IsAlive(IInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            var unityObject = interactable as Object;
            return ReferenceEquals(unityObject, null) || unityObject != null;
        }
    }
}
