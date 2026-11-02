using LaundryLegends.NPC;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Brings arcade visitors to active machines on a cadence. Only one visitor is
    /// in the shop at a time, so the income flow stays readable. The cadence is
    /// shortened by footfall, upgrades and the number of active machines, but never
    /// below a floor, and a caller-supplied base interval is never lengthened.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeTraffic : MonoBehaviour
    {
        private const float MinimumIntervalSeconds = 6f;

        [SerializeField] private ArcadeService _service;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _cabinet;
        [SerializeField] private GameObject _visitorPrefab;
        [SerializeField, Min(1f)] private float _intervalSeconds = 20f;
        [SerializeField, Min(0.1f)] private float _playSeconds = 6f;
        [SerializeField, Min(1)] private int _income = 3;
        [SerializeField, Min(0.1f)] private float _speed = 1.4f;

        private ArcadeVisitor _visitor;
        private float _timer;
        private bool _incomeOverrideSet;
        private int _slotOverride = -1;

        public int VisitorsSpawned { get; private set; }

        public ArcadeVisitor ActiveVisitor => _visitor;

        public int ActiveVisitorSlot => _visitor != null ? _visitor.SlotIndex : -1;

        public float BaseIntervalSeconds => _intervalSeconds;

        /// <summary>The cadence actually used, after footfall, upgrades and machines.</summary>
        public float EffectiveIntervalSeconds
        {
            get
            {
                if (_service == null)
                {
                    return _intervalSeconds;
                }

                float factor = _service.TrafficFactor;
                if (factor <= 0.01f)
                {
                    return _intervalSeconds;
                }

                float computed = _intervalSeconds / factor;

                // Never make an authored/test interval longer than requested, and
                // never let the scene cadence collapse below the balance floor.
                return Mathf.Min(_intervalSeconds, Mathf.Max(MinimumIntervalSeconds, computed));
            }
        }

        public void Configure(ArcadeService service, Transform spawnPoint, Transform cabinet, GameObject visitorPrefab = null)
        {
            _service = service;
            _spawnPoint = spawnPoint;
            _cabinet = cabinet;
            _visitorPrefab = visitorPrefab;
        }

        public void SetInterval(float seconds)
        {
            _intervalSeconds = Mathf.Max(1f, seconds);
        }

        public void SetIncome(int income)
        {
            _income = Mathf.Max(1, income);
            _incomeOverrideSet = true;
        }

        public void SetPlaySeconds(float seconds)
        {
            _playSeconds = Mathf.Max(0.1f, seconds);
        }

        /// <summary>Forces every new visitor to one slot; useful for deterministic tests.</summary>
        public void SetSlotOverride(int slotIndex)
        {
            _slotOverride = slotIndex;
        }

        public void ClearSlotOverride()
        {
            _slotOverride = -1;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances the arrival clock. Public so tests can simulate minutes of
        /// traffic without waiting.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (_service == null || !_service.HasActiveMachine)
            {
                _timer = 0f;
                return;
            }

            if (_visitor != null)
            {
                if (!_visitor.IsFinished)
                {
                    return;
                }

                DestroyObject(_visitor.gameObject);
                _visitor = null;
                _timer = 0f;
                return;
            }

            _timer += Mathf.Max(0f, deltaSeconds);
            if (_timer >= EffectiveIntervalSeconds)
            {
                SpawnVisitor();
            }
        }

        /// <summary>Spawns one visitor for an automatically chosen active machine.</summary>
        public ArcadeVisitor SpawnVisitor()
        {
            return SpawnVisitor(ChooseSlot());
        }

        /// <summary>Spawns one visitor for a specific slot; used by tests and the scene.</summary>
        public ArcadeVisitor SpawnVisitor(int slotIndex)
        {
            if (_visitor != null)
            {
                return _visitor;
            }

            if (_service == null)
            {
                return null;
            }

            if (slotIndex < 0 || !_service.IsSlotActive(slotIndex))
            {
                slotIndex = FirstActiveSlot();
                if (slotIndex < 0)
                {
                    return null;
                }
            }

            Transform target = _service.GetSlotCabinetTransform(slotIndex);
            if (target == null)
            {
                target = _cabinet;
            }

            Vector3 origin = _spawnPoint != null ? _spawnPoint.position : transform.position;
            GameObject instance = _visitorPrefab != null
                ? Instantiate(_visitorPrefab, origin, Quaternion.identity)
                : CreatePlaceholderBody(origin);

            instance.name = $"ArcadeVisitor_s{slotIndex}";

            // Disable the borrowed NPC's gameplay scripts while inactive, then add or
            // reuse the visitor behaviour. Only locomotion and the visitor itself stay
            // enabled, so a customer prefab cannot start a laundry order or a second
            // visitor loop.
            instance.SetActive(false);
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is NpcLocomotion || behaviour is ArcadeVisitor)
                {
                    continue;
                }

                behaviour.enabled = false;
            }

            _visitor = instance.GetComponent<ArcadeVisitor>();
            if (_visitor == null)
            {
                _visitor = instance.AddComponent<ArcadeVisitor>();
            }

            _visitor.enabled = true;
            _visitor.Configure(
                _service,
                target,
                transform.position,
                GetIncomeFor(slotIndex),
                _playSeconds,
                _speed,
                slotIndex);
            instance.SetActive(true);
            VisitorsSpawned += 1;
            _timer = 0f;
            return _visitor;
        }

        private int ChooseSlot()
        {
            if (_slotOverride >= 0)
            {
                return _slotOverride;
            }

            int first = -1;
            float totalWeight = 0f;
            for (int i = 0; i < _service.SlotCount; i++)
            {
                if (!_service.IsSlotActive(i))
                {
                    continue;
                }

                first = first < 0 ? i : first;
                ArcadeMachineDefinition definition = _service.GetSlotDefinition(i);
                totalWeight += definition != null ? definition.AttractWeight : 1f;
            }

            if (first < 0)
            {
                return -1;
            }

            if (totalWeight <= 0.01f)
            {
                return first;
            }

            float roll = Random.Range(0f, totalWeight);
            float running = 0f;
            for (int i = 0; i < _service.SlotCount; i++)
            {
                if (!_service.IsSlotActive(i))
                {
                    continue;
                }

                ArcadeMachineDefinition definition = _service.GetSlotDefinition(i);
                running += definition != null ? definition.AttractWeight : 1f;
                if (roll <= running)
                {
                    return i;
                }
            }

            return first;
        }

        private int FirstActiveSlot()
        {
            for (int i = 0; i < _service.SlotCount; i++)
            {
                if (_service.IsSlotActive(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetIncomeFor(int slotIndex)
        {
            if (slotIndex == 0 && _incomeOverrideSet)
            {
                return _income;
            }

            return _service != null ? _service.GetVisitIncome(slotIndex) : _income;
        }

        private static GameObject CreatePlaceholderBody(Vector3 origin)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.position = origin + new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);

            Collider collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyObject(collider);
            }

            return body;
        }

        /// <summary>
        /// Destroys immediately outside play mode, where <see cref="Object.Destroy"/>
        /// is rejected by Unity.
        /// </summary>
        private static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
