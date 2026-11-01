using LaundryLegends.NPC;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Brings arcade visitors to an active machine on a fixed cadence. Only one
    /// visitor is in the shop at a time, so the income flow stays readable and
    /// the shop never fills with idle bodies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeTraffic : MonoBehaviour
    {
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

        public int VisitorsSpawned { get; private set; }

        public ArcadeVisitor ActiveVisitor => _visitor;

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
        }

        public void SetPlaySeconds(float seconds)
        {
            _playSeconds = Mathf.Max(0.1f, seconds);
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
            if (_service == null || !_service.IsActive)
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
                return;            }

            _timer += Mathf.Max(0f, deltaSeconds);
            if (_timer >= _intervalSeconds)
            {
                SpawnVisitor();
            }
        }

        /// <summary>Spawns one visitor immediately; used by <see cref="Tick"/> and tests.</summary>
        public ArcadeVisitor SpawnVisitor()
        {
            if (_visitor != null)
            {
                return _visitor;
            }

            Vector3 origin = _spawnPoint != null ? _spawnPoint.position : transform.position;
            GameObject instance = _visitorPrefab != null
                ? Instantiate(_visitorPrefab, origin, Quaternion.identity)
                : CreatePlaceholderBody(origin);

            instance.name = "ArcadeVisitor";

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
            _visitor.Configure(_service, _cabinet, transform.position, _income, _playSeconds, _speed);
            instance.SetActive(true);
            VisitorsSpawned += 1;
            _timer = 0f;
            return _visitor;
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
