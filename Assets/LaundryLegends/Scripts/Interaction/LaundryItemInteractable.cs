using LaundryLegends.Laundry;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// Pickup boundary for one laundry prop. It reports the item kind, rejects
    /// items that are already held or placed in a slot, and delegates ownership
    /// to <see cref="CarryController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LaundryItem))]
    public sealed class LaundryItemInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private LaundryItem _item;

        public LaundryItem Item => _item != null ? _item : GetComponent<LaundryItem>();

        public string InteractionPrompt
        {
            get
            {
                LaundryItem item = Item;
                if (item == null)
                {
                    return "Pick up laundry";
                }

                switch (item.Kind)
                {
                    case LaundryItemKind.Basket:
                        return "Pick up basket";
                    case LaundryItemKind.Bag:
                        return "Pick up bag";
                    default:
                        return "Pick up laundry";
                }
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            LaundryItem item = Item;
            if (!isActiveAndEnabled
                || item == null
                || !item.IsCarryable
                || !item.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (GetComponentInParent<CarryController>() != null)
            {
                return false;
            }

            if (GetComponentInParent<CarrySlot>() != null)
            {
                return false;
            }

            return interactor != null && interactor.GetComponent<CarryController>() != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            CarryController carry = interactor.GetComponent<CarryController>();
            carry.TryPickUp(Item);
        }

        /// <summary>Authoring/test wiring for the item reference.</summary>
        public void Configure(LaundryItem item)
        {
            _item = item;
        }
    }
}
