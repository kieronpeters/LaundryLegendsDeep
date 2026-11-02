using System;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Laundry reputation, 0-100. Paid orders and tips raise it, abandoned orders
    /// lower it, and neglect decays it slowly. Footfall maps the value onto the
    /// arcade traffic multiplier.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LaundryReputation : MonoBehaviour
    {
        public const int MaxValue = 100;
        public const int PaidOrderChange = 4;
        public const int TipChange = 2;
        public const int AbandonedOrderChange = -2;

        [SerializeField, Range(0, MaxValue)] private int _value;
        [SerializeField, Min(1f)] private float _decaySecondsPerPoint = 120f;

        private float _decayTimer;

        public event Action<int> Changed;

        public int Value => _value;

        /// <summary>0.8 at zero reputation, 1.4 at full reputation.</summary>
        public float FootfallMultiplier => 0.8f + 0.6f * (_value / (float)MaxValue);

        public float DecaySecondsPerPoint => _decaySecondsPerPoint;

        public void SetValue(int value)
        {
            int clamped = Mathf.Clamp(value, 0, MaxValue);
            if (clamped == _value)
            {
                return;
            }

            _value = clamped;
            Changed?.Invoke(_value);
        }

        public void RecordPaidOrder()
        {
            Add(PaidOrderChange);
        }

        public void RecordTip()
        {
            Add(TipChange);
        }

        public void RecordAbandonedOrder()
        {
            Add(AbandonedOrderChange);
        }

        /// <summary>Advances the decay clock. Public so tests can simulate neglect.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_value <= 0 || deltaSeconds <= 0f)
            {
                return;
            }

            _decayTimer += deltaSeconds;
            float interval = Mathf.Max(1f, _decaySecondsPerPoint);
            while (_decayTimer >= interval && _value > 0)
            {
                _decayTimer -= interval;
                _value -= 1;
                Changed?.Invoke(_value);
            }
        }

        public void SetDecaySecondsPerPoint(float seconds)
        {
            _decaySecondsPerPoint = Mathf.Max(1f, seconds);
        }

        private void Add(int delta)
        {
            SetValue(_value + delta);
        }
    }
}
