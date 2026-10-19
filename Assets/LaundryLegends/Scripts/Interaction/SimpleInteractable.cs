using System;
using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// Minimal interactable used by placeholder props and interaction tests.
    /// </summary>
    public sealed class SimpleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Interact";

        public string InteractionPrompt => _prompt;

        public event Action<GameObject> Interacted;

        public void Interact(GameObject interactor)
        {
            Interacted?.Invoke(interactor);
        }
    }
}
