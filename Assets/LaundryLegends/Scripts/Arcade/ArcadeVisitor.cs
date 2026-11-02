using LaundryLegends.NPC;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>Where one arcade visitor is in its short life.</summary>
    public enum ArcadeVisitorState
    {
        Approaching,
        Playing,
        Leaving,
        Finished
    }

    /// <summary>
    /// One arcade customer: walks to its machine, plays for a bounded time, pays
    /// one visit's income, then leaves. The visit id is created once when play
    /// starts and passed to <see cref="ArcadeService"/>, so a retried payment can
    /// never credit twice.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeVisitor : MonoBehaviour
    {
        [SerializeField] private ArcadeService _service;
        [SerializeField] private Transform _cabinet;
        [SerializeField] private Vector3 _exitPoint;
        [SerializeField, Min(0)] private int _income = 3;
        [SerializeField, Min(0.1f)] private float _playSeconds = 6f;
        [SerializeField, Min(0.1f)] private float _speed = 1.4f;
        [SerializeField, Min(0.1f)] private float _arriveDistance = 1.1f;
        [SerializeField, Min(0)] private int _slotIndex;

        private NpcLocomotion _locomotion;
        private float _playTimer;

        public ArcadeVisitorState State { get; private set; } = ArcadeVisitorState.Approaching;

        public bool IsFinished => State == ArcadeVisitorState.Finished;

        /// <summary>Stable id for this visit; empty until play begins.</summary>
        public string VisitId { get; private set; } = string.Empty;

        /// <summary>True once this visit has been paid exactly once.</summary>
        public bool WasPaid { get; private set; }

        public int Income => _income;

        public int SlotIndex => _slotIndex;

        public void Configure(
            ArcadeService service,
            Transform cabinet,
            Vector3 exitPoint,
            int income = 3,
            float playSeconds = 6f,
            float speed = 1.4f)
        {
            Configure(service, cabinet, exitPoint, income, playSeconds, speed, 0);
        }

        public void Configure(
            ArcadeService service,
            Transform cabinet,
            Vector3 exitPoint,
            int income,
            float playSeconds,
            float speed,
            int slotIndex)
        {
            _service = service;
            _cabinet = cabinet;
            _exitPoint = exitPoint;
            _income = Mathf.Max(0, income);
            _playSeconds = Mathf.Max(0.1f, playSeconds);
            _speed = Mathf.Max(0.1f, speed);
            _slotIndex = Mathf.Max(0, slotIndex);
        }

        private void Awake()
        {
            _locomotion = GetComponent<NpcLocomotion>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances one step. Public so tests can run a whole visit without waiting
        /// real seconds.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            float step = Mathf.Max(0f, deltaSeconds);

            switch (State)
            {
                case ArcadeVisitorState.Approaching:
                    if (_cabinet == null || Arrive(_cabinet.position, step))
                    {
                        BeginPlay();
                    }

                    break;

                case ArcadeVisitorState.Playing:
                    _playTimer += step;
                    if (_playTimer >= _playSeconds)
                    {
                        CompleteVisit();
                    }

                    break;

                case ArcadeVisitorState.Leaving:
                    if (Arrive(_exitPoint, step))
                    {
                        State = ArcadeVisitorState.Finished;
                    }

                    break;
            }
        }

        private void BeginPlay()
        {
            if (_service == null || !_service.IsSlotActive(_slotIndex))
            {
                // The machine went offline before this visitor arrived.
                State = ArcadeVisitorState.Leaving;
                return;
            }

            VisitId = _service.CreateVisitId(_slotIndex);
            _playTimer = 0f;
            State = ArcadeVisitorState.Playing;
        }

        private void CompleteVisit()
        {
            if (_service != null && !string.IsNullOrEmpty(VisitId))
            {
                WasPaid = _service.TryCollectVisitIncome(_slotIndex, VisitId, _income, Time.time);
            }

            State = ArcadeVisitorState.Leaving;
        }

        private bool Arrive(Vector3 target, float step)
        {
            Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);

            // Move by the supplied step rather than by Time.deltaTime so a whole visit
            // can be simulated deterministically in tests.
            transform.position = Vector3.MoveTowards(transform.position, flatTarget, _speed * step);

            if (_locomotion != null)
            {
                _locomotion.LookAt(flatTarget);
            }

            return Vector3.Distance(transform.position, flatTarget) <= _arriveDistance;
        }
    }
}
