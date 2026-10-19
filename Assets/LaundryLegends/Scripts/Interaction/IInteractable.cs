using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// Contract for props the player can look at and use.
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }

        void Interact(GameObject interactor);
    }
}
