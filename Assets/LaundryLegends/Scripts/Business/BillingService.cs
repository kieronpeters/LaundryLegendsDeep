using System;
using System.Collections.Generic;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using UnityEngine;

namespace LaundryLegends.Business
{
    [Serializable]
    public sealed class BillLine
    {
        [SerializeField] private string _label;
        [SerializeField, Min(0)] private int _amount;

        public string Label => _label;
        public int Amount => _amount;

        public BillLine()
        {
        }

        public BillLine(string label, int amount)
        {
            _label = label;
            _amount = Mathf.Max(0, amount);
        }
    }

    /// <summary>One itemized bill. Serializable so Phase 14 can persist it.</summary>
    [Serializable]
    public sealed class UtilityBill
    {
        [SerializeField] private int _dueDay;
        [SerializeField] private bool _paid;
        [SerializeField] private bool _lateFeeApplied;
        [SerializeField] private List<BillLine> _lines = new List<BillLine>();

        public int DueDay => _dueDay;
        public bool IsPaid => _paid;
        public bool LateFeeApplied => _lateFeeApplied;
        public IReadOnlyList<BillLine> Lines => _lines;

        public int Total
        {
            get
            {
                int total = 0;
                foreach (BillLine line in _lines)
                {
                    total += line.Amount;
                }

                return total;
            }
        }

        public void Initialize(int dueDay)
        {
            _dueDay = Mathf.Max(0, dueDay);
            _paid = false;
            _lateFeeApplied = false;
            _lines = new List<BillLine>();
        }

        public void AddLine(string label, int amount)
        {
            _lines.Add(new BillLine(label, amount));
        }

        public void MarkPaid()
        {
            _paid = true;
        }

        public void ApplyLateFee(int amount)
        {
            if (_lateFeeApplied)
            {
                return;
            }

            _lateFeeApplied = true;
            AddLine("Late fee", amount);
        }
    }

    /// <summary>
    /// Generates an itemized rent/electricity/water bill every 30 in-game days,
    /// applies one late fee after the grace period, and pays only when the wallet
    /// can cover it. Never drives the wallet negative.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BillingService : MonoBehaviour
    {
        [SerializeField] private BusinessClock _clock;
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField, Min(0)] private int _rent = 250;
        [SerializeField, Min(0)] private int _electricity = 90;
        [SerializeField, Min(0)] private int _water = 60;
        [SerializeField, Min(1)] private int _periodDays = 30;
        [SerializeField, Min(0)] private int _graceDays = 3;
        [SerializeField, Range(0f, 1f)] private float _lateFeeRate = 0.1f;
        [SerializeField] private List<UtilityBill> _bills = new List<UtilityBill>();

        public event Action<UtilityBill> BillGenerated;
        public event Action<UtilityBill> BillPaid;

        public IReadOnlyList<UtilityBill> Bills => _bills;

        public int PeriodDays => _periodDays;
        public int GraceDays => _graceDays;
        public int Rent => _rent;
        public int Electricity => _electricity;
        public int Water => _water;

        public UtilityBill CurrentBill
        {
            get
            {
                foreach (UtilityBill bill in _bills)
                {
                    if (!bill.IsPaid)
                    {
                        return bill;
                    }
                }

                return null;
            }
        }

        public int TotalDue
        {
            get
            {
                int total = 0;
                foreach (UtilityBill bill in _bills)
                {
                    if (!bill.IsPaid)
                    {
                        total += bill.Total;
                    }
                }

                return total;
            }
        }

        public string LastResult { get; private set; } = string.Empty;

        public CustomerSpawner Spawner => _spawner;

        public void Configure(BusinessClock clock, CustomerSpawner spawner)
        {
            Unsubscribe();
            _clock = clock;
            _spawner = spawner;
            Subscribe();
        }

        public void ConfigureAmounts(int rent, int electricity, int water, int periodDays, int graceDays, float lateFeeRate)
        {
            _rent = Mathf.Max(0, rent);
            _electricity = Mathf.Max(0, electricity);
            _water = Mathf.Max(0, water);
            _periodDays = Mathf.Max(1, periodDays);
            _graceDays = Mathf.Max(0, graceDays);
            _lateFeeRate = Mathf.Clamp01(lateFeeRate);
        }

        private void Start()
        {
            EnsureClock();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>Creates a bill for the given day. Public for tests and closeout.</summary>
        public UtilityBill GenerateBill(int dueDay)
        {
            var bill = new UtilityBill();
            bill.Initialize(dueDay);
            bill.AddLine("Rent", _rent);
            bill.AddLine("Electricity", _electricity);
            bill.AddLine("Water", _water);
            _bills.Add(bill);
            BillGenerated?.Invoke(bill);
            LastResult = $"Bill ${bill.Total} due day {dueDay}";
            return bill;
        }

        public bool TryPayCurrent(EconomyService economy, float atSeconds)
        {
            return TryPay(CurrentBill, economy, atSeconds);
        }

        public bool TryPayCurrentFromSpawner(float atSeconds)
        {
            return TryPayCurrent(_spawner != null ? _spawner.Economy : null, atSeconds);
        }

        public bool TryPay(UtilityBill bill, EconomyService economy, float atSeconds)
        {
            if (bill == null || bill.IsPaid)
            {
                LastResult = "No unpaid bill";
                return false;
            }

            if (economy == null)
            {
                LastResult = "No economy service";
                return false;
            }

            if (!economy.TrySpend(bill.Total, "Rent and utilities", atSeconds))
            {
                LastResult = $"Not enough money for the ${bill.Total} bill";
                return false;
            }

            bill.MarkPaid();
            BillPaid?.Invoke(bill);
            LastResult = $"Paid ${bill.Total} bill";
            return true;
        }

        /// <summary>Applies one late fee per unpaid bill after its grace period.</summary>
        public void ApplyLateFees(int day)
        {
            foreach (UtilityBill bill in _bills)
            {
                if (bill.IsPaid || bill.LateFeeApplied || day <= bill.DueDay + _graceDays)
                {
                    continue;
                }

                int fee = Mathf.RoundToInt(bill.Total * _lateFeeRate);
                if (fee <= 0)
                {
                    continue;
                }

                bill.ApplyLateFee(fee);
                LastResult = $"Late fee ${fee} added to the day {bill.DueDay} bill";
            }
        }

        private void OnDayChanged(int day)
        {
            if (day % _periodDays == 0)
            {
                GenerateBill(day);
            }

            ApplyLateFees(day);
        }

        private void EnsureClock()
        {
            if (_clock == null)
            {
                _clock = FindFirstObjectByType<BusinessClock>();
            }

            if (_spawner == null)
            {
                _spawner = FindFirstObjectByType<CustomerSpawner>();
            }
        }

        private void Subscribe()
        {
            if (_clock != null)
            {
                _clock.DayChanged -= OnDayChanged;
                _clock.DayChanged += OnDayChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_clock != null)
            {
                _clock.DayChanged -= OnDayChanged;
            }
        }
    }
}
