using System;
using LaundryLegends.Audio;
using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// Resolves the interactable under the player's view, exposes its prompt, and
    /// dispatches the interaction key. Out-of-range, blocked, ineligible, and
    /// paused targets are never reported or dispatched.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range = 3f;
        [SerializeField] private LayerMask _interactionMask = ~0;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        /// <summary>Raised when the reported interactable or its prompt changes.</summary>
        public event Action TargetChanged;

        public IInteractable CurrentInteractable { get; private set; }

        public GameObject CurrentTarget { get; private set; }

        public string CurrentPrompt { get; private set; }

        /// <summary>Single source of truth for the interaction input.</summary>
        public KeyCode InteractKey => _interactKey;

        /// <summary>Display label for the interaction input; UI must not hard-code a key.</summary>
        public string InteractKeyLabel => _interactKey.ToString();

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                ClearTarget();
                return;
            }

            RefreshTarget();

            if (CurrentInteractable != null && Input.GetKeyDown(_interactKey))
            {
                DispatchInteraction();
            }
        }

        /// <summary>
        /// Re-resolves the current target and performs it. Returns true only when an
        /// eligible interaction actually dispatched. Safe to call from tests or a
        /// future input layer that owns key handling.
        /// </summary>
        public bool TryInteract()
        {
            if (Time.timeScale == 0f)
            {
                ClearTarget();
                return false;
            }

            RefreshTarget();
            return DispatchInteraction();
        }

        private void RefreshTarget()
        {
            if (_camera == null)
            {
                ClearTarget();
                return;
            }

            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, _range, _interactionMask, QueryTriggerInteraction.Ignore))
            {
                ClearTarget();
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (!IsAlive(interactable) || !interactable.CanInteract(gameObject))
            {
                ClearTarget();
                return;
            }

            SetTarget(interactable, hit.collider.gameObject);
        }

        private bool DispatchInteraction()
        {
            IInteractable interactable = CurrentInteractable;
            if (!IsAlive(interactable))
            {
                ClearTarget();
                return false;
            }

            if (!interactable.CanInteract(gameObject))
            {
                ClearTarget();
                return false;
            }

            interactable.Interact(gameObject);
            GameAudio.Play(Sfx.Click);
            return true;
        }

        private void SetTarget(IInteractable interactable, GameObject target)
        {
            string prompt = interactable.InteractionPrompt;
            if (ReferenceEquals(CurrentInteractable, interactable)
                && CurrentTarget == target
                && CurrentPrompt == prompt)
            {
                return;
            }

            CurrentInteractable = interactable;
            CurrentTarget = target;
            CurrentPrompt = prompt;
            TargetChanged?.Invoke();
        }

        private void ClearTarget()
        {
            if (CurrentInteractable == null && CurrentTarget == null && CurrentPrompt == null)
            {
                return;
            }

            CurrentInteractable = null;
            CurrentTarget = null;
            CurrentPrompt = null;
            TargetChanged?.Invoke();
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

            var unityObject = interactable as UnityEngine.Object;
            return ReferenceEquals(unityObject, null) || unityObject != null;
        }
    }
}
