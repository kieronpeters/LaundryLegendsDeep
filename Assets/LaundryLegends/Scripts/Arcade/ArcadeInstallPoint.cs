using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// The purchase point for an arcade machine slot. It performs no economy or
    /// state work itself; it only offers the interaction and defers to
    /// <see cref="ArcadeService"/>, which owns the validation and the debit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeInstallPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private ArcadeService _service;
        [SerializeField, Min(0)] private int _slotIndex;
        [SerializeField] private string _prompt = "Install arcade machine";

        public ArcadeService Service => _service;

        public int SlotIndex => _slotIndex;

        /// <summary>True while there is still a machine to buy and the anchor is open.</summary>
        public bool IsAvailable =>
            _service != null
            && !_service.IsSlotInstalled(_slotIndex)
            && _service.IsSlotReachable(_slotIndex);

        public void SetService(ArcadeService service)
        {
            SetSlot(service, 0);
        }

        /// <summary>Wires this point to one service slot.</summary>
        public void SetSlot(ArcadeService service, int slotIndex, string prompt = null)
        {
            _service = service;
            _slotIndex = Mathf.Max(0, slotIndex);
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
                    return "Install point (unwired)";
                }

                if (_service.IsSlotInstalled(_slotIndex))
                {
                    return "Arcade machine installed";
                }

                if (!_service.IsSlotReachable(_slotIndex))
                {
                    return "Arcade machine anchor (sealed)";
                }

                ArcadeMachineDefinition definition = _service.GetSlotDefinition(_slotIndex);
                string machine = definition != null ? definition.DisplayName : "Arcade";
                string price = $"${_service.GetSlotCost(_slotIndex)}";
                string blocker = _service.DescribeInstallBlocker(_slotIndex);
                return string.IsNullOrEmpty(blocker)
                    ? $"{_prompt}: {machine} ({price})"
                    : $"{_prompt}: {machine} ({price}) - {blocker}";
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

            _service.TryInstall(_slotIndex, Time.time);
        }
    }
}
