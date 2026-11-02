using LaundryLegends.Audio;
using LaundryLegends.Business;
using LaundryLegends.Interaction;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.Laundry
{
    /// <summary>
    /// Folding/wrapping boundary: accepts one dried loose item, marks it wrapped,
    /// and leaves it on the table for customer pickup. No payment or order flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoldingStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private CarrySlot _slot;
        [SerializeField] private ConsumableStockService _stock;

        public LaundryItem ReadyItem => _slot != null ? _slot.Occupant : null;
        public bool IsReady => ReadyItem != null && ReadyItem.Stage == LaundryStage.Wrapped;
        public string LastResult { get; private set; } = string.Empty;

        public string InteractionPrompt => IsReady ? "Wrapped laundry ready" : "Fold and wrap";

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && _slot != null;
        }

        public void Configure(CarrySlot slot)
        {
            _slot = slot;
        }

        public void SetStockService(ConsumableStockService stock)
        {
            _stock = stock;
        }

        private void Awake()
        {
            if (_stock == null)
            {
                _stock = FindFirstObjectByType<ConsumableStockService>();
            }
        }

        /// <summary>Staff path: wrap a dried item and leave it for pickup.</summary>
        public bool WrapItem(LaundryItem item)
        {
            if (item == null || item.Stage != LaundryStage.Dried || _slot == null || !_slot.IsEmpty)
            {
                return false;
            }

            if (!TryConsumePlastic())
            {
                return false;
            }

            if (!_slot.TryPlace(item))
            {
                return false;
            }

            item.SetStage(LaundryStage.Wrapped);
            GameAudio.Play(Sfx.Wrap);
            LastResult = "Folded and wrapped";
            return true;
        }

        /// <summary>Pickup path: remove the wrapped bundle.</summary>
        public LaundryItem TakeWrappedItem()
        {
            return _slot != null ? _slot.Take() : null;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null || _slot == null || IsReady)
            {
                return;
            }

            CarryController carry = interactor.GetComponent<CarryController>();
            if (carry == null || carry.HeldItem == null)
            {
                LastResult = "Nothing to fold";
                return;
            }

            LaundryItem item = carry.HeldItem;
            if (item.Stage != LaundryStage.Dried)
            {
                LastResult = "Laundry must be dried first";
                return;
            }

            if (!TryConsumePlastic())
            {
                return;
            }

            if (!carry.TryPlaceInto(_slot))
            {
                LastResult = "Table is occupied";
                return;
            }

            item.SetStage(LaundryStage.Wrapped);
            GameAudio.Play(Sfx.Wrap);
            LastResult = "Folded and wrapped";
            Debug.Log($"[Folding] {LastResult}.");
        }

        private bool TryConsumePlastic()
        {
            if (_stock == null)
            {
                return true;
            }

            if (_stock.TryConsume(ConsumableType.Plastic))
            {
                return true;
            }

            LastResult = $"Out of {_stock.DisplayName(ConsumableType.Plastic)}";
            return false;
        }
    }
}
