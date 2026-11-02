using LaundryLegends.Arcade;
using LaundryLegends.Business;
using LaundryLegends.Customers;
using UnityEngine;

namespace LaundryLegends.UI
{
    /// <summary>
    /// Compact business HUD: day/time, stock, active/ready orders and the next
    /// bill. N skips to the next morning for testing the 30-day cycle.
    /// </summary>
    public sealed class BusinessPanelView : MonoBehaviour
    {
        [SerializeField] private BusinessClock _clock;
        [SerializeField] private ConsumableStockService _stock;
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField] private BillingService _billing;
        [SerializeField] private DropOffRack _rack;
        [SerializeField] private float _width = 760f;
        [SerializeField] private float _height = 102f;

        public void Configure(
            BusinessClock clock,
            ConsumableStockService stock,
            CustomerSpawner spawner,
            BillingService billing,
            DropOffRack rack)
        {
            _clock = clock;
            _stock = stock;
            _spawner = spawner;
            _billing = billing;
            _rack = rack;
        }

        public string BuildText()
        {
            string time = _clock != null ? _clock.DisplayText : "Day 1  08:00";
            string detergent = _stock != null ? $"{_stock.Count(ConsumableType.Detergent)}/{_stock.Capacity(ConsumableType.Detergent)}" : "-";
            string fragrance = _stock != null ? $"{_stock.Count(ConsumableType.Fragrance)}/{_stock.Capacity(ConsumableType.Fragrance)}" : "-";
            string plastic = _stock != null ? $"{_stock.Count(ConsumableType.Plastic)}/{_stock.Capacity(ConsumableType.Plastic)}" : "-";
            int active = _spawner != null ? _spawner.ActiveCustomers.Count : 0;
            int ready = _rack != null ? _rack.ReadyCount : 0;
            string bill = "none";
            if (_billing != null)
            {
                UtilityBill current = _billing.CurrentBill;
                bill = current != null ? $"${current.Total} due day {current.DueDay}" : "none";
            }

            return
                $"{time}   N next day\n" +
                $"Stock  Detergent {detergent}   Fragrance {fragrance}   Plastic {plastic}\n" +
                $"Orders  {active} active / {ready} ready\n" +
                $"Next bill  {bill}   [B] bills";
        }

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Resolve();
        }

        private void Update()
        {
            Resolve();
            if (_clock == null || ArcadeMode.IsActive || Time.timeScale <= 0f)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                _clock.SkipToNextDay();
            }
        }

        private void OnGUI()
        {
            if (_clock == null || ArcadeMode.IsActive || Time.timeScale == 0f)
            {
                return;
            }

            float left = Mathf.Max(8f, (Screen.width - _width) * 0.5f);
            GUI.Box(new Rect(left, 16f, _width, _height), "BUSINESS");
            GUI.Label(new Rect(left + 12f, 40f, _width - 24f, _height - 46f), BuildText());
        }

        private void Resolve()
        {
            if (_clock == null) _clock = FindFirstObjectByType<BusinessClock>();
            if (_stock == null) _stock = FindFirstObjectByType<ConsumableStockService>();
            if (_spawner == null) _spawner = FindFirstObjectByType<CustomerSpawner>();
            if (_billing == null) _billing = FindFirstObjectByType<BillingService>();
            if (_rack == null) _rack = FindFirstObjectByType<DropOffRack>();
        }
    }
}
