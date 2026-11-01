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

        /// <summary>
        /// Hands the upgrade to the shared capability map. Both catalogue
        /// upgrades are machine-speed effects, and each machine ignores a repeat
        /// application, so re-running this after a reload is safe.
        /// </summary>
        private static void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            UpgradeEffects.Apply(
                upgrade,
                UnityEngine.Object.FindObjectsByType<MachineLoadHook>(FindObjectsSortMode.None));
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

            EnsureEconomy();

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
            agent.Configure(_ledger, _orderSlot, _doorPoint, _counterPoint, _exitPoint, item);
            agent.StateEntered += OnCustomerState;

            ActiveCustomer = agent;
            _waitingToRespawn = false;
            Debug.Log("[CUSTOMER] A customer walked in.");
            return agent;
        }

        public void DisposeCurrent()
        {
            if (ActiveCustomer == null)
            {
                return;
            }

            ActiveCustomer.StateEntered -= OnCustomerState;
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
            BeginRespawn();
        }

        /// <summary>Arms the respawn delay. Re-arming restarts the timer.</summary>
        private void BeginRespawn()
        {
            _waitingToRespawn = true;
            _respawnTimer = Mathf.Max(0f, _respawnDelay);
            Debug.Log($"[CUSTOMER] Next customer in {_respawnTimer:0.0}s.");
        }

        /// <summary>
        /// Serves one customer at a time and never lets a missing or interrupted
        /// customer stop the shop. If the active customer disappears without
        /// reaching Done, it is cleared and the respawn timer starts, so the loop
        /// continues instead of stalling after one order.
        /// </summary>
        private void Update()
        {
            if (!_autoStart)
            {
                return;
            }

            if (ActiveCustomer == null)
            {
                if (!_waitingToRespawn)
                {
                    BeginRespawn();
                }
            }
            else if (!ActiveCustomer.isActiveAndEnabled || ActiveCustomer.State == CustomerState.Done)
            {
                Debug.LogWarning("[CUSTOMER] The active customer stopped without reporting Done; clearing it and respawning.");
                DisposeCurrent();
                BeginRespawn();
                return;
            }

            if (!_waitingToRespawn)
            {
                return;
            }

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer > 0f)
            {
                return;
            }

            _waitingToRespawn = false;
            if (SpawnNow() == null)
            {
                // Missing wiring must not turn into a spawn attempt every frame.
                BeginRespawn();
            }
        }
    }
}
