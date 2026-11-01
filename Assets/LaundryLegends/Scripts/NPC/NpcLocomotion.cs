using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Bridges transform-driven NPC movement to the humanoid Animator.
    /// Writes a normalized Speed parameter and the Carrying flag, and offers
    /// helpers so scripted NPCs can turn and step toward a point.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcLocomotion : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int CarryingHash = Animator.StringToHash("Carrying");

        [SerializeField] private Animator _animator;
        [SerializeField] private float _referenceSpeed = 1.1f;
        [SerializeField] private float _turnDegreesPerSecond = 540f;

        private Vector3 _lastPosition;
        private bool _carrying;

        public float Speed01 { get; private set; }

        public bool Carrying
        {
            get => _carrying;
            set
            {
                if (_carrying == value)
                {
                    return;
                }

                _carrying = value;
                if (_animator != null && _animator.runtimeAnimatorController != null)
                {
                    _animator.SetBool(CarryingHash, value);
                }
            }
        }

        public bool HasAnimator => _animator != null && _animator.runtimeAnimatorController != null;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            _lastPosition = transform.position;
        }

        private void Update()
        {
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;

            float distance = new Vector2(delta.x, delta.z).magnitude;
            float speed = distance / Mathf.Max(Time.deltaTime, 0.0001f);
            Speed01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, _referenceSpeed));

            if (HasAnimator)
            {
                _animator.SetFloat(SpeedHash, Speed01, 0.1f, Time.deltaTime);
                _animator.SetBool(CarryingHash, _carrying);
            }
        }

        /// <summary>Step toward a world point at a constant speed, facing the direction of travel.</summary>
        public bool MoveTowards(Vector3 destination, float speed, float arriveDistance = 0.05f)
        {
            Vector3 flat = new Vector3(destination.x, transform.position.y, destination.z);
            Vector3 before = transform.position;
            Vector3 step = Vector3.MoveTowards(transform.position, flat, Mathf.Max(0f, speed) * Time.deltaTime) - transform.position;

            // A CharacterController makes the NPC collide with counters and machines
            // instead of walking through them; without one it still moves directly.
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null && controller.enabled)
            {
                controller.Move(step);
            }
            else
            {
                transform.position = before + step;
            }

            Vector3 direction = flat - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Face(direction);
            }

            return Vector3.Distance(transform.position, flat) <= arriveDistance;
        }

        /// <summary>Turn toward a world point without moving.</summary>
        public void LookAt(Vector3 worldPoint)
        {
            Vector3 direction = worldPoint - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Face(direction);
            }
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                _turnDegreesPerSecond * Time.deltaTime);
        }
    }
}
