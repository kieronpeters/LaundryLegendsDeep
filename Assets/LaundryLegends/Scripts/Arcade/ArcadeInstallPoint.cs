using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// The purchase point for the first arcade machine. It performs no economy or
    /// state work itself; it only offers the interaction and defers to
    /// <see cref="ArcadeService"/>, which owns the validation and the debit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeInstallPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private ArcadeService _service;
        [SerializeField] private string _prompt = "Install arcade machine";

        public ArcadeService Service => _service;

        /// <summary>True while there is still a machine to buy.</summary>
        public bool IsAvailable => _service != null && !_service.IsInstalled;

        public void SetService(ArcadeService service)
        {
            _service = service;
        }

        public string InteractionPrompt
        {
            get
            {
                if (_service == null)
                {
                    return "Install point (unwired)";
                }

                if (_service.IsInstalled)
                {
                    return "Arcade machine installed";
                }

                string price = $"${_service.MachineCost}";
                string blocker = _service.DescribeInstallBlocker();
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

            _service.TryInstall(Time.time);
        }
    }
}
