using LaundryLegends.Interaction;
using LaundryLegends.Progression;
using UnityEngine;

namespace LaundryLegends.Progression
{
    public enum SecretRoomAction
    {
        CollectClue,
        Reveal,
        Prepare
    }

    /// <summary>
    /// Thin interaction bridge for the secret room: one component covers clue
    /// pickups, the reveal attempt and each preparation step.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SecretRoomInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private SecretRoomController _controller;
        [SerializeField] private SecretRoomAction _action = SecretRoomAction.CollectClue;
        [SerializeField] private string _prompt = "Inspect";

        public void Configure(SecretRoomController controller, SecretRoomAction action, string prompt)
        {
            _controller = controller;
            _action = action;
            _prompt = prompt;
        }

        public string InteractionPrompt => _prompt;

        public bool CanInteract(GameObject interactor)
        {
            if (_controller == null)
            {
                return false;
            }

            switch (_action)
            {
                case SecretRoomAction.Reveal:
                    return _controller.Stage == SecretRoomStage.Hidden && _controller.IsEligible;
                case SecretRoomAction.Prepare:
                    return _controller.IsRevealed && _controller.Stage != SecretRoomStage.Usable;
                default:
                    return true;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (_controller == null)
            {
                return;
            }

            switch (_action)
            {
                case SecretRoomAction.CollectClue:
                    if (_controller.TryCollectClue())
                    {
                        gameObject.SetActive(false);
                    }

                    break;
                case SecretRoomAction.Reveal:
                    _controller.TryReveal();
                    break;
                case SecretRoomAction.Prepare:
                    _controller.TryPrepare();
                    break;
            }
        }
    }
}
