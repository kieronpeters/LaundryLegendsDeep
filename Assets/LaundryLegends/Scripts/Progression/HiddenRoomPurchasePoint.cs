using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Progression
{
    /// <summary>
    /// The shelf in front of the back room. Paying the price clears it, opens the
    /// door and prepares the room; the arcade cabinet inside is then reachable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HiddenRoomPurchasePoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private SecretRoomController _room;
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField] private int _cost = 200;
        [SerializeField] private string _prompt = "Clear shelf and open back room";

        public int Cost => _cost;

        public bool IsPurchased => _room != null && _room.IsRevealed;

        public void SetRoom(SecretRoomController room)
        {
            _room = room;
        }

        public void SetSpawner(CustomerSpawner spawner)
        {
            _spawner = spawner;
        }

        public string InteractionPrompt
        {
            get
            {
                if (_room == null)
                {
                    return "Shelf (unwired)";
                }

                if (IsPurchased)
                {
                    return "Back room open";
                }

                EconomyService economy = Economy;
                int balance = economy != null ? economy.Balance : 0;
                return balance >= _cost
                    ? $"{_prompt} (${_cost})"
                    : $"{_prompt} (${_cost}, need ${_cost - balance} more)";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && _room != null && !IsPurchased;
        }

        public void Interact(GameObject interactor)
        {
            TryPurchase(Time.time);
        }

        /// <summary>
        /// Spends the price once and unlocks the room. Refused without side effects
        /// when already open or unaffordable.
        /// </summary>
        public bool TryPurchase(float atSeconds)
        {
            if (_room == null || IsPurchased)
            {
                return false;
            }

            EconomyService economy = Economy;
            if (economy == null || economy.Balance < _cost)
            {
                return false;
            }

            if (!economy.TrySpend(_cost, "Clear the shelf", atSeconds))
            {
                return false;
            }

            _room.UnlockByPurchase();
            return true;
        }

        private EconomyService Economy => _spawner != null ? _spawner.Economy : null;
    }
}
