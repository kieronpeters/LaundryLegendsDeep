using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Sells the visible second bay. The service owns the purchase, the debit and
    /// the geometry switch; this component only offers the interaction.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeExpansionPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private ArcadeService _service;
        [SerializeField] private string _prompt = "Open arcade expansion bay";

        public ArcadeService Service => _service;

        public bool IsAvailable => _service != null && !_service.IsExpanded;

        public void SetService(ArcadeService service, string prompt = null)
        {
            _service = service;
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                _prompt = prompt;
            }
        }

        public string InteractionPrompt
        {
            get
            {
                if (_service == null)
                {
                    return "Expansion plan (unwired)";
                }

                if (_service.IsExpanded)
                {
                    return "Expansion bay open";
                }

                string blocker = _service.DescribeExpansionBlocker();
                string price = $"${_service.ExpansionCost}";
                return string.IsNullOrEmpty(blocker)
                    ? $"{_prompt} ({price})"
                    : $"{_prompt} ({price}) - {blocker}";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && IsAvailable;
        }

        public void Interact(GameObject interactor)
        {
            if (_service == null)
            {
                return;
            }

            _service.TryExpand(Time.time);
        }
    }
}
