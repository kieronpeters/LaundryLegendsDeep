using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// Contract for props the player can look at and use.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short action text shown to the player while this is targeted.</summary>
        string InteractionPrompt { get; }

        /// <summary>Whether this interactable currently accepts the given interactor.</summary>
        bool CanInteract(GameObject interactor);

        /// <summary>Performs the interaction. Implementations must no-op when not eligible.</summary>
        void Interact(GameObject interactor);
    }
}
