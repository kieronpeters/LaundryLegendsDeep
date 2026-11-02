using System;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Business
{
    /// <summary>
    /// One consumable shelf. Interacting buys a single unit for the definition's
    /// fixed price; the stack under <see cref="_stackRoot"/> is shown or hidden so
    /// the purchase is visible in the room. Stock is serializable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConsumableShelf : MonoBehaviour, IInteractable
    {
        [SerializeField] private ConsumableDefinition _definition;
        [SerializeField, Min(0)] private int _count;
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField] private Transform _stackRoot;

        public event Action<ConsumableShelf> Changed;

        public ConsumableDefinition Definition => _definition;
        public int Count => _count;
        public int Capacity => _definition != null ? _definition.Capacity : 10;
        public bool IsFull => _count >= Capacity;
        public bool IsEmpty => _count <= 0;
        public string LastResult { get; private set; } = string.Empty;

        public void Configure(
            ConsumableDefinition definition,
            CustomerSpawner spawner,
            Transform stackRoot,
            int startingCount)
        {
            _definition = definition;
            _spawner = spawner;
            _stackRoot = stackRoot;
            _count = Mathf.Clamp(startingCount, 0, Capacity);
            RefreshVisuals();
        }

        public void SetSpawner(CustomerSpawner spawner)
        {
            _spawner = spawner;
        }

        public void SetStackRoot(Transform stackRoot)
        {
            _stackRoot = stackRoot;
            RefreshVisuals();
        }

        public void SetCount(int count)
        {
            int clamped = Mathf.Clamp(count, 0, Capacity);
            if (clamped == _count)
            {
                return;
            }

            _count = clamped;
            RefreshVisuals();
            Changed?.Invoke(this);
        }

        /// <summary>Buys one unit. A full shelf or a failed debit changes nothing.</summary>
        public bool TryBuyOne(EconomyService economy, float atSeconds)
        {
            if (_definition == null)
            {
                LastResult = "Shelf has no item definition";
                return false;
            }

            if (IsFull)
            {
                LastResult = $"{_definition.DisplayName} shelf is full";
                return false;
            }

            if (economy == null)
            {
                LastResult = "No economy service";
                return false;
            }

            if (!economy.TrySpend(_definition.UnitCost, $"Stock: {_definition.DisplayName}", atSeconds))
            {
                LastResult = $"Not enough money for {_definition.DisplayName}";
                return false;
            }

            SetCount(_count + 1);
            LastResult = $"{_definition.DisplayName} {_count}/{Capacity}";
            return true;
        }

        /// <summary>Consumes one unit for a processing stage. Fails when empty.</summary>
        public bool TryConsumeOne()
        {
            if (IsEmpty)
            {
                LastResult = $"Out of {(_definition != null ? _definition.DisplayName : "stock")}";
                return false;
            }

            SetCount(_count - 1);
            LastResult = $"{(_definition != null ? _definition.DisplayName : "Stock")} {_count}/{Capacity}";
            return true;
        }

        public string InteractionPrompt
        {
            get
            {
                if (_definition == null)
                {
                    return "Stock shelf (unwired)";
                }

                if (IsFull)
                {
                    return $"{_definition.DisplayName} {_count}/{Capacity} (full)";
                }

                return $"Buy {_definition.DisplayName} (${_definition.UnitCost})  {_count}/{Capacity}";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && _definition != null && !IsFull;
        }

        public void Interact(GameObject interactor)
        {
            if (_spawner == null)
            {
                _spawner = FindFirstObjectByType<CustomerSpawner>();
            }

            TryBuyOne(_spawner != null ? _spawner.Economy : null, Time.time);
            Debug.Log($"[SHELF] {LastResult}");
        }

        /// <summary>Shows the first <see cref="Count"/> stack items and hides the rest.</summary>
        public void RefreshVisuals()
        {
            if (_stackRoot == null)
            {
                return;
            }

            for (int i = 0; i < _stackRoot.childCount; i++)
            {
                Transform child = _stackRoot.GetChild(i);
                bool visible = i < _count;
                if (child.gameObject.activeSelf != visible)
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }
    }
}
