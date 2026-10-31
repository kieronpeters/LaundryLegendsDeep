using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Minimal ambient NPC loop: walks a fixed route, waits, speaks, and leaves
    /// with a carried laundry prop. No NavMesh, orders, or economy behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbientNpc : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _speed = 1.1f;
        [SerializeField] private float _stopDuration = 3f;
        [SerializeField] private float _respawnDelay = 3f;
        [SerializeField] private GameObject _carriedProp;
        [SerializeField] private AudioSource _voice;
        [SerializeField] private AudioClip _enterClip;
        [SerializeField] private AudioClip _leaveClip;
        [SerializeField] private bool _loop = true;
        [SerializeField] private CarrySlot _dropOffSlot;

        private int _index;
        private float _timer;
        private bool _waiting;
        private bool _hidden;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Renderer[] _renderers = new Renderer[0];
        private Collider _collider;

        public int WaypointCount => _waypoints != null ? _waypoints.Length : 0;

        public GameObject CarriedProp => _carriedProp;

        public AudioSource Voice => _voice;

        /// <summary>Editor/test wiring for the route and voice.</summary>
        public void Configure(
            Transform[] waypoints,
            GameObject carriedProp,
            AudioSource voice,
            AudioClip enterClip,
            AudioClip leaveClip)
        {
            _waypoints = waypoints;
            _carriedProp = carriedProp;
            _voice = voice;
            _enterClip = enterClip;
            _leaveClip = leaveClip;
        }

        public void ConfigureDropOff(CarrySlot slot)
        {
            _dropOffSlot = slot;
        }

        private void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _collider = GetComponent<Collider>();
        }

        private void Start()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                enabled = false;
                return;
            }

            Speak(_enterClip);
            SetCarriedProp(true);
            _index = _waypoints.Length > 1 ? 1 : 0;
        }

        private void Update()
        {
            if (_hidden || _waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            if (_waiting)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _waiting = false;
                    _index++;
                    if (_index >= _waypoints.Length)
                    {
                        StartCoroutine(LeaveAndRespawn());
                        return;
                    }
                }

                return;
            }

            Transform target = _waypoints[_index];
            Vector3 flatTarget = new Vector3(target.position.x, transform.position.y, target.position.z);
            transform.position = Vector3.MoveTowards(transform.position, flatTarget, _speed * Time.deltaTime);
            Vector3 direction = flatTarget - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            if (Vector3.Distance(transform.position, flatTarget) <= 0.08f)
            {
                _waiting = true;
                _timer = _stopDuration;
                if (_index == 2)
                {
                    DropCarriedProp();
                    Speak(_leaveClip);
                }
            }
        }

        private IEnumerator LeaveAndRespawn()
        {
            _hidden = true;
            SetCarriedProp(false);
            SetVisible(false);
            yield return new WaitForSeconds(_respawnDelay);

            if (!_loop)
            {
                yield break;
            }

            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _index = _waypoints.Length > 1 ? 1 : 0;
            _waiting = false;
            _timer = 0f;
            _hidden = false;
            SetVisible(true);
            SetCarriedProp(true);
            Speak(_enterClip);
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }

            if (_collider != null)
            {
                _collider.enabled = visible;
            }
        }

        private void SetCarriedProp(bool visible)
        {
            if (_carriedProp != null)
            {
                _carriedProp.SetActive(visible);
            }
        }

        private void DropCarriedProp()
        {
            if (_dropOffSlot == null || _carriedProp == null || !_carriedProp.activeSelf)
            {
                return;
            }

            LaundryItem item = _carriedProp.GetComponent<LaundryItem>();
            if (item != null && _dropOffSlot.TryPlace(item))
            {
                item.SetStage(LaundryStage.Dirty);
                _carriedProp = null;
            }
        }

        private void Speak(AudioClip clip)
        {
            if (_voice != null && clip != null)
            {
                _voice.PlayOneShot(clip);
            }
        }
    }
}
