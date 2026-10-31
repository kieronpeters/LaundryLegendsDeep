using LaundryLegends.Interaction;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.Laundry
{
    /// <summary>
    /// Phase 3 washer boundary: accepts one loose-laundry item and returns the
    /// same instance on unload. This is a load/unload hook only; cycles, timers,
    /// water, soap, and payment belong to later phases.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MachineLoadHook : MonoBehaviour, IInteractable
    {
        [SerializeField] private CarrySlot _slot;
        [SerializeField] private string _emptyPrompt = "Load washer";
        [SerializeField] private string _occupiedPrompt = "Take laundry";

        /// <summary>Last interaction result, exposed for tests and placeholder feedback.</summary>
        public string LastResult { get; private set; } = string.Empty;

        public CarrySlot Slot => _slot;

        public LaundryItem LoadedItem => _slot != null ? _slot.Occupant : null;

        public bool IsLoaded => LoadedItem != null;

        public string InteractionPrompt => IsLoaded ? _occupiedPrompt : _emptyPrompt;

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && _slot != null;
        }

        /// <summary>Authoring/test wiring for the load slot; setup tools save the reference.</summary>
        public void Configure(CarrySlot slot)
        {
            _slot = slot;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null || _slot == null)
            {
                return;
            }

            CarryController carry = interactor.GetComponent<CarryController>();
            if (carry == null)
            {
                LastResult = "Interactor cannot carry laundry";
                Debug.Log($"[Machine] {LastResult}.");
                return;
            }

            if (IsLoaded)
            {
                TryUnload(carry);
            }
            else
            {
                TryLoad(carry);
            }

            Debug.Log($"[Machine] {LastResult}.");
        }

        /// <summary>
        /// Moves the carried item into the machine. Rejects a missing item, an
        /// occupied machine, or anything that is not washer-loadable, without
        /// changing the item's owner.
        /// </summary>
        public bool TryLoad(CarryController carry)
        {
            if (_slot == null)
            {
                LastResult = "Machine has no load slot";
                return false;
            }

            if (carry == null)
            {
                LastResult = "Interactor cannot carry laundry";
                return false;
            }

            if (IsLoaded)
            {
                LastResult = "Machine already loaded";
                return false;
            }

            LaundryItem item = carry.HeldItem;
            if (item == null)
            {
                LastResult = "Nothing to load";
                return false;
            }

            if (!item.IsWasherLoadable)
            {
                LastResult = $"{item.Kind} cannot go in the washer; only loose laundry";
                return false;
            }

            bool placed = carry.TryPlaceInto(_slot);
            LastResult = placed ? "Loaded loose laundry" : "Machine rejected the load";
            return placed;
        }

        /// <summary>
        /// Returns the exact stored instance to an empty-handed carrier. Rejects
        /// an empty machine or a carrier that is already holding something, and
        /// leaves the slot unchanged on rejection.
        /// </summary>
        public LaundryItem TryUnload(CarryController carry)
        {
            if (_slot == null)
            {
                LastResult = "Machine has no load slot";
                return null;
            }

            if (carry == null)
            {
                LastResult = "Interactor cannot carry laundry";
                return null;
            }

            LaundryItem item = LoadedItem;
            if (item == null)
            {
                LastResult = "Machine empty";
                return null;
            }

            if (carry.IsCarrying)
            {
                LastResult = "Hands full; put the held item down first";
                return null;
            }

            LaundryItem taken = _slot.Take();
            if (taken == null)
            {
                LastResult = "Machine empty";
                return null;
            }

            if (!carry.TryPickUp(taken))
            {
                _slot.TryPlace(taken);
                LastResult = "Unload rejected";
                return null;
            }

            LastResult = "Unloaded loose laundry";
            return taken;
        }
    }
}
