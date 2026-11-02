using LaundryLegends.Arcade;
using LaundryLegends.Business;
using LaundryLegends.Core;
using LaundryLegends.Economy;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Customers
{
    /// <summary>
    /// Spawns order customers up to a configurable maximum and gives each one its
    /// own counter slot. Multiple customers can be in the shop at once, each with
    /// a ticket; the staff processes their slots in order and returns each wrapped
    /// parcel to the same slot for pickup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _customerPrefab;
        [SerializeField] private GameObject _laundryPrefab;
        [SerializeField] private CarrySlot _orderSlot;
        [SerializeField] private DropOffRack _orderRack;
        [SerializeField] private Transform _streetPoint;
        [SerializeField] private Transform _doorPoint;
        [SerializeField] private Transform _counterPoint;
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private float _respawnDelay = 4f;
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private int _price = 40;
        [SerializeField, Min(1)] private int _maxCustomers = 10;
        [SerializeField] private LaundryReputation _reputation;

        private OrderLedger _ledger;
        private EconomyService _economy;
        private readonly List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>(UpgradeCatalogue.CreateDefault());
        private readonly List<CustomerAgent> _activeCustomers = new List<CustomerAgent>();
        private float _respawnTimer;

        /// <summary>The first active customer, kept for older tests and callers.</summary>
        public CustomerAgent ActiveCustomer =>
            _activeCustomers.Count > 0 ? _activeCustomers[0] : null;

        public IReadOnlyList<CustomerAgent> ActiveCustomers => _activeCustomers;

        public int MaxCustomers => _maxCustomers;

        public DropOffRack OrderRack => _orderRack;

        public OrderLedger Ledger => _ledger;

        public EconomyService Economy
        {
            get
            {
                EnsureEconomy();
                return _economy;
            }
        }

        public IReadOnlyList<UpgradeDefinition> Upgrades => _upgrades;

        /// <summary>Laundry reputation fed by paid orders, tips and abandons.</summary>
        public LaundryReputation Reputation
        {
            get
            {
                if (_reputation == null)
                {
                    _reputation = FindFirstObjectByType<LaundryReputation>();
                }

                return _reputation;
            }
        }

        public void SetReputation(LaundryReputation reputation)
        {
            _reputation = reputation;
            WireReputation();
        }

        public void SetOrderRack(DropOffRack rack)
        {
            _orderRack = rack;
        }

        public void SetAutoStart(bool autoStart)
        {
            _autoStart = autoStart;
        }

        public void SetMaxCustomers(int maximum)
        {
            _maxCustomers = Mathf.Max(1, maximum);
        }

        public void Initialize(OrderLedger ledger, bool autoStart)
        {
            _ledger = ledger;
            _autoStart = autoStart;
            WireReputation();
        }

        public void SetRespawnDelay(float seconds)
        {
            _respawnDelay = Mathf.Max(0f, seconds);
            _respawnTimer = Mathf.Min(_respawnTimer, _respawnDelay);
        }

        private void Start()
        {
            EnsureEconomy();

            if (_autoStart)
            {
                _respawnTimer = SpawnNow() != null ? Mathf.Max(0f, _respawnDelay) : 0.5f;
            }
        }

        /// <summary>
        /// Owns exactly one economy service. If a ledger was injected without
        /// one, the ledger keeps its legacy direct-credit path.
        /// </summary>
        private void EnsureEconomy()
        {
            if (_economy != null)
            {
                return;
            }

            if (_ledger != null && _ledger.Economy != null)
            {
                _economy = _ledger.Economy;
            }
            else
            {
                var wallet = _ledger != null
                    ? _ledger.Wallet
                    : (GameSession.Instance != null ? GameSession.Instance.State : new GameState(0));
                _economy = new EconomyService(wallet);
                if (_ledger == null)
                {
                    _ledger = new OrderLedger(wallet, _economy);
                }
            }

            _economy.UpgradePurchased += OnUpgradePurchased;
            ApplyOwnedUpgrades();
            WireReputation();
        }

        private void WireReputation()
        {
            if (_ledger != null && Reputation != null)
            {
                _ledger.SetReputation(Reputation);
            }
        }

        /// <summary>Buys the cheapest unowned upgrade from the catalogue.</summary>
        public bool TryPurchaseNextUpgrade()
        {
            EnsureEconomy();
            UpgradeDefinition next = _economy.FindNextGoal(_upgrades);
            return next != null && _economy.TryPurchase(next, Time.time);
        }

        /// <summary>Buys a specific laundry upgrade from the purchase menu.</summary>
        public bool TryPurchaseUpgrade(UpgradeDefinition upgrade)
        {
            EnsureEconomy();
            return upgrade != null && _economy.TryPurchase(upgrade, Time.time);
        }

        private void OnUpgradePurchased(UpgradeDefinition upgrade)
        {
            ApplyUpgrade(upgrade);
        }

        private void ApplyOwnedUpgrades()
        {
            foreach (UpgradeDefinition upgrade in _upgrades)
            {
                if (_economy != null && _economy.HasUpgrade(upgrade.Id))
                {
                    ApplyUpgrade(upgrade);
                }
            }
        }

        private static void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            UpgradeEffects.Apply(
                upgrade,
                UnityEngine.Object.FindObjectsByType<MachineLoadHook>(FindObjectsSortMode.None));
        }

        /// <summary>
        /// Spawns one customer if the shop has room and a counter slot is free.
        /// Returns null when at capacity or there is nowhere to put the order.
        /// </summary>
        public CustomerAgent SpawnNow()
        {
            if (_activeCustomers.Count >= _maxCustomers)
            {
                return null;
            }

            if (_customerPrefab == null || _streetPoint == null)
            {
                Debug.LogWarning("[CUSTOMER] Spawner is missing its prefab or street point.");
                return null;
            }

            EnsureEconomy();

            CarrySlot slot;
            if (_orderRack != null)
            {
                slot = _orderRack.ReserveFreeSlot();
                if (slot == null)
                {
                    return null;
                }
            }
            else
            {
                slot = _orderSlot;
                if (slot == null || !slot.IsEmpty)
                {
                    return null;
                }
            }

            GameObject instance = Instantiate(_customerPrefab, _streetPoint.position, _streetPoint.rotation);
            instance.name = "OrderCustomer";

            // The customer must not body-block the player in the doorway. A Character
            // Controller can never be a trigger, so it is left alone.
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                if (collider is CharacterController)
                {
                    continue;
                }

                collider.isTrigger = true;
            }

            var agent = instance.GetComponent<CustomerAgent>();
            if (agent == null)
            {
                agent = instance.AddComponent<CustomerAgent>();
            }

            LaundryItem item = CreateLaundryItem(instance.transform);
            agent.Configure(_ledger, slot, _doorPoint, _counterPoint, _exitPoint, item);
            agent.StateEntered += OnCustomerState;

            _activeCustomers.Add(agent);
            Debug.Log($"[CUSTOMER] A customer walked in ({_activeCustomers.Count}/{_maxCustomers}).");
            return agent;
        }

        public void DisposeCurrent()
        {
            if (_activeCustomers.Count == 0)
            {
                return;
            }

            RemoveCustomer(_activeCustomers[0], true);
        }

        public void DisposeAll()
        {
            for (int i = _activeCustomers.Count - 1; i >= 0; i--)
            {
                RemoveCustomer(_activeCustomers[i], true);
            }
        }

        private void RemoveCustomer(CustomerAgent agent, bool destroy)
        {
            if (agent == null)
            {
                return;
            }

            agent.StateEntered -= OnCustomerState;
            CarrySlot slot = agent.OrderSlot;
            _orderRack?.Release(slot);
            if (slot != null)
            {
                LaundryItem leftover = slot.Take();
                if (leftover != null)
                {
                    Destroy(leftover.gameObject);
                }
            }

            _activeCustomers.Remove(agent);

            if (destroy && agent.gameObject != null)
            {
                Destroy(agent.gameObject);
            }
        }

        private LaundryItem CreateLaundryItem(Transform parent)
        {
            if (_laundryPrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(_laundryPrefab, parent.position, parent.rotation);
            instance.name = "CustomerLaundry";

            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            return instance.GetComponent<LaundryItem>();
        }

        private void OnCustomerState(CustomerAgent agent, CustomerState state)
        {
            if (state != CustomerState.Done)
            {
                return;
            }

            RemoveCustomer(agent, true);
        }

        private void CleanActive()
        {
            for (int i = _activeCustomers.Count - 1; i >= 0; i--)
            {
                CustomerAgent agent = _activeCustomers[i];
                if (agent == null || !agent.isActiveAndEnabled || agent.State == CustomerState.Done)
                {
                    RemoveCustomer(agent, agent != null);
                }
            }
        }

        /// <summary>
        /// Keeps the shop stocked with customers up to the configured maximum. The
        /// timer is shared, so one customer arrives per respawn interval.
        /// </summary>
        private void Update()
        {
            if (!_autoStart)
            {
                return;
            }

            CleanActive();

            if (_activeCustomers.Count >= _maxCustomers)
            {
                return;
            }

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer > 0f)
            {
                return;
            }

            _respawnTimer = SpawnNow() != null ? Mathf.Max(0f, _respawnDelay) : 0.5f;
        }
    }
}
