using System;
using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// Minimal interactable used by placeholder props and interaction tests.
    /// Eligible by default; can be disabled through the component or the flag.
    /// </summary>
    public sealed class SimpleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Interact";
        [SerializeField] private bool _interactable = true;

        public string InteractionPrompt => _prompt;

        /// <summary>Serialized eligibility flag, changeable by tests and future props.</summary>
        public bool IsInteractable
        {
            get => _interactable;
            set => _interactable = value;
        }

        public event Action<GameObject> Interacted;

        public bool CanInteract(GameObject interactor)
        {
            return _interactable && isActiveAndEnabled;
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            Interacted?.Invoke(interactor);
        }
    }
}
