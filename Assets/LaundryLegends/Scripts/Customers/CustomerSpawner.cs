using LaundryLegends.Core;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
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
        private float _respawnTimer;
        private bool _waitingToRespawn;

        public CustomerAgent ActiveCustomer { get; private set; }

        public OrderLedger Ledger => _ledger;

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
            if (_ledger == null)
            {
                var wallet = GameSession.Instance != null ? GameSession.Instance.State : new GameState(0);
                _ledger = new OrderLedger(wallet);
            }

            if (_autoStart)
            {
                SpawnNow();
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
