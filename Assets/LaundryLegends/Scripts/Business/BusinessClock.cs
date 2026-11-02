using System;
using UnityEngine;

namespace LaundryLegends.Business
{
    /// <summary>Serializable clock state so a later save can restore the calendar.</summary>
    [Serializable]
    public sealed class BusinessClockState
    {
        [SerializeField] private int _day = 1;
        [SerializeField, Range(0, 1439)] private int _minuteOfDay = 480;

        public int Day => _day;
        public int MinuteOfDay => _minuteOfDay;

        public void Set(int day, int minuteOfDay)
        {
            _day = Mathf.Max(1, day);
            _minuteOfDay = Mathf.Clamp(minuteOfDay, 0, 1439);
        }

        /// <summary>
        /// Advances by whole game minutes, rolling over at midnight. Returns the
        /// number of day boundaries crossed.
        /// </summary>
        public int Advance(int minutes)
        {
            if (minutes <= 0)
            {
                return 0;
            }

            int total = _minuteOfDay + minutes;
            int days = total / 1440;
            _minuteOfDay = total % 1440;
            _day += days;
            return days;
        }

        public void SkipToNextMorning()
        {
            _day += 1;
            _minuteOfDay = 480;
        }
    }

    /// <summary>
    /// Day 1 onward, 24-hour business clock. One real second is one game minute by
    /// default; the skip action lets a playtest reach a 30-day bill without waiting
    /// twelve real hours. The clock is economic: lighting stays static for now.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BusinessClock : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _startDay = 1;
        [SerializeField, Range(0, 1439)] private int _startMinute = 480;
        [SerializeField, Min(0.01f)] private float _realSecondsPerGameMinute = 1f;
        [SerializeField] private BusinessClockState _state = new BusinessClockState();

        private float _minuteAccumulator;

        /// <summary>Raised for each new day that begins, with the new day number.</summary>
        public event Action<int> DayChanged;

        public int Day
        {
            get
            {
                EnsureState();
                return _state.Day;
            }
        }

        public int MinuteOfDay
        {
            get
            {
                EnsureState();
                return _state.MinuteOfDay;
            }
        }

        public int Hour => MinuteOfDay / 60;
        public int Minute => MinuteOfDay % 60;

        public string DisplayText => $"Day {Day}  {Hour:00}:{Minute:00}";

        public float RealSecondsPerGameMinute => _realSecondsPerGameMinute;

        public BusinessClockState State
        {
            get
            {
                EnsureState();
                return _state;
            }
        }

        public void Configure(int startDay, int startMinute, float realSecondsPerGameMinute)
        {
            _startDay = Mathf.Max(1, startDay);
            _startMinute = Mathf.Clamp(startMinute, 0, 1439);
            _realSecondsPerGameMinute = Mathf.Max(0.01f, realSecondsPerGameMinute);
            _state = new BusinessClockState();
            _state.Set(_startDay, _startMinute);
            _minuteAccumulator = 0f;
        }

        public void SetPace(float realSecondsPerGameMinute)
        {
            _realSecondsPerGameMinute = Mathf.Max(0.01f, realSecondsPerGameMinute);
        }

        public void SetTime(int day, int minuteOfDay)
        {
            EnsureState();
            _state.Set(day, minuteOfDay);
            _minuteAccumulator = 0f;
        }

        /// <summary>Advances real time. Public so tests can run days deterministically.</summary>
        public void Tick(float realSeconds)
        {
            if (realSeconds <= 0f)
            {
                return;
            }

            _minuteAccumulator += realSeconds / Mathf.Max(0.01f, _realSecondsPerGameMinute);
            int minutes = Mathf.FloorToInt(_minuteAccumulator);
            if (minutes <= 0)
            {
                return;
            }

            _minuteAccumulator -= minutes;
            AdvanceMinutes(minutes);
        }

        /// <summary>Advances whole game minutes and raises DayChanged for each rollover.</summary>
        public void AdvanceMinutes(int minutes)
        {
            EnsureState();
            int before = _state.Day;
            _state.Advance(minutes);
            for (int day = before + 1; day <= _state.Day; day++)
            {
                DayChanged?.Invoke(day);
            }
        }

        /// <summary>Jumps to 08:00 the next day; used by the counter/office action and tests.</summary>
        public void SkipToNextDay()
        {
            EnsureState();
            _state.SkipToNextMorning();
            _minuteAccumulator = 0f;
            DayChanged?.Invoke(_state.Day);
        }

        private void Awake()
        {
            EnsureState();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void EnsureState()
        {
            if (_state == null)
            {
                _state = new BusinessClockState();
                _state.Set(_startDay, _startMinute);
            }
        }
    }
}
