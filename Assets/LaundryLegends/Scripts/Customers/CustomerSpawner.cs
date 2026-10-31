using LaundryLegends.Core;
using LaundryLegends.Economy;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Customers
{
    /// <summary>
    /// Spawns one order customer at a time, hands it a fresh laundry item and
    /// the shared ledger, and respawns after the previous customer leaves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _customerPrefab;
        [SerializeField] private GameObject _laundryPrefab;
        [SerializeField] private CarrySlot _orderSlot;
        [SerializeField] private Transform _streetPoint;
        [SerializeField] private Transform _doorPoint;
        [SerializeField] private Transform _counterPoint;
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private float _respawnDelay = 4f;
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private int _price = 40;

        private OrderLedger _ledger;
        private EconomyService _economy;
        private readonly List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>(UpgradeCatalogue.CreateDefault());
        private float _respawnTimer;
        private bool _waitingToRespawn;

        public CustomerAgent ActiveCustomer { get; private set; }

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

        public void Initialize(OrderLedger ledger, bool autoStart)
        {
            _ledger = ledger;
            _autoStart = autoStart;
        }

        public void SetRespawnDelay(float seconds)
        {
            _respawnDelay = Mathf.Max(0f, seconds);
        }

        private void Start()
        {
            EnsureEconomy();

            if (_autoStart)
            {
                SpawnNow();
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
        }

        /// <summary>Buys the cheapest unowned upgrade from the catalogue.</summary>
        public bool TryPurchaseNextUpgrade()
        {
            EnsureEconomy();
            UpgradeDefinition next = _economy.FindNextGoal(_upgrades);
            return next != null && _economy.TryPurchase(next, Time.time);
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
            if (upgrade == null || upgrade.Id != UpgradeCatalogue.FastDryerId)
            {
                return;
            }

            foreach (MachineLoadHook machine in UnityEngine.Object.FindObjectsByType<MachineLoadHook>(
                         FindObjectsSortMode.None))
            {
                if (machine.Kind == MachineKind.Dryer)
                {
                    machine.ApplyUpgrade(upgrade.Id, UpgradeCatalogue.FastDryerDurationMultiplier);
                }
            }
        }

        public CustomerAgent SpawnNow()
        {
            if (ActiveCustomer != null)
            {
                return ActiveCustomer;
            }

            if (_customerPrefab == null || _streetPoint == null)
            {
                Debug.LogWarning("[CUSTOMER] Spawner is missing its prefab or street point.");
                return null;
            }

            GameObject instance = Instantiate(_customerPrefab, _streetPoint.position, _streetPoint.rotation);
            instance.name = "OrderCustomer";

            // The customer must not body-block the player in the doorway.
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                collider.isTrigger = true;
            }

            var agent = instance.GetComponent<CustomerAgent>();
            if (agent == null)
            {
                agent = instance.AddComponent<CustomerAgent>();
            }

            LaundryItem item = CreateLaundryItem(instance.transform);
            agent.Configure(_ledger, _orderSlot, _doorPoint, _counterPoint, _exitPoint, item);
            agent.StateEntered += OnCustomerState;

            ActiveCustomer = agent;
            return agent;
        }

        public void DisposeCurrent()
        {
            if (ActiveCustomer == null)
            {
                return;
            }

            Destroy(ActiveCustomer.gameObject);
            ActiveCustomer = null;
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

            agent.StateEntered -= OnCustomerState;
            Destroy(agent.gameObject);
            ActiveCustomer = null;
            _waitingToRespawn = true;
            _respawnTimer = _respawnDelay;
        }

        private void Update()
        {
            if (!_waitingToRespawn)
            {
                return;
            }

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                _waitingToRespawn = false;
                if (_autoStart)
                {
                    SpawnNow();
                }
            }
        }
    }
}
