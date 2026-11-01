using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// A two-handed carry anchor. While an item is attached it follows this
    /// transform, ignores physics, and reports the carrying state to
    /// <see cref="NpcLocomotion"/> so the animation set can switch. Detaching
    /// restores the colliders and the exact Rigidbody flags the item had.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarrySocket : MonoBehaviour
    {
        [SerializeField] private Transform _anchor;
        [SerializeField] private NpcLocomotion _locomotion;
        [SerializeField] private Vector3 _carryLocalPosition = new Vector3(0f, 0.05f, 0.22f);

        private LaundryItem _carried;
        private Transform _originalParent;
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private Collider[] _colliders = new Collider[0];
        private Rigidbody[] _bodies = new Rigidbody[0];
        private bool[] _bodyKinematic = new bool[0];
        private bool[] _bodyGravity = new bool[0];

        public LaundryItem Carried => _carried;

        public bool IsCarrying => _carried != null;

        private Transform _fallbackAnchor;

        public Transform Anchor => ResolvedAnchor;

        /// <summary>
        /// Falls back to a chest-height anchor on the NPC rather than the root, so an
        /// unauthored socket still holds the laundry in front of the body instead of
        /// at the feet.
        /// </summary>
        private Transform ResolvedAnchor
        {
            get
            {
                if (_anchor != null)
                {
                    return _anchor;
                }

                if (_fallbackAnchor == null)
                {
                    var anchor = new GameObject("CarryAnchor");
                    anchor.transform.SetParent(transform, false);
                    anchor.transform.localPosition = new Vector3(0f, 1.15f, 0.3f);
                    _fallbackAnchor = anchor.transform;
                }

                return _fallbackAnchor;
            }
        }

        private void Awake()
        {
            if (_locomotion == null)
            {
                _locomotion = GetComponentInParent<NpcLocomotion>();
            }
        }

        public void Configure(Transform anchor)
        {
            _anchor = anchor;
        }

        public bool Attach(LaundryItem item)
        {
            if (item == null || _carried != null)
            {
                return false;
            }

            _carried = item;
            _originalParent = item.transform.parent;
            _originalLocalPosition = item.transform.localPosition;
            _originalLocalRotation = item.transform.localRotation;

            _bodies = item.GetComponentsInChildren<Rigidbody>(true);
            _bodyKinematic = new bool[_bodies.Length];
            _bodyGravity = new bool[_bodies.Length];
            for (int i = 0; i < _bodies.Length; i++)
            {
                Rigidbody body = _bodies[i];
                _bodyKinematic[i] = body.isKinematic;
                _bodyGravity[i] = body.useGravity;
                body.isKinematic = true;
                body.useGravity = false;
            }

            _colliders = item.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in _colliders)
            {
                collider.enabled = false;
            }

            item.transform.SetParent(ResolvedAnchor, false);
            item.transform.localPosition = _carryLocalPosition;
            item.transform.localRotation = Quaternion.identity;

            if (_locomotion != null)
            {
                _locomotion.Carrying = true;
            }

            return true;
        }

        public LaundryItem Detach()
        {
            LaundryItem item = _carried;
            if (item == null)
            {
                return null;
            }

            _carried = null;

            foreach (Collider collider in _colliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            _colliders = new Collider[0];

            if (item.transform.parent == ResolvedAnchor)
            {
                item.transform.SetParent(_originalParent, false);
                item.transform.localPosition = _originalLocalPosition;
                item.transform.localRotation = _originalLocalRotation;
            }

            // Restore physics only after the item is back in its own space so a
            // re-enabled body resumes where the NPC let go of it.
            for (int i = 0; i < _bodies.Length; i++)
            {
                Rigidbody body = _bodies[i];
                if (body == null)
                {
                    continue;
                }

                body.isKinematic = _bodyKinematic[i];
                body.useGravity = _bodyGravity[i];
            }

            _bodies = new Rigidbody[0];
            _bodyKinematic = new bool[0];
            _bodyGravity = new bool[0];

            if (_locomotion != null)
            {
                _locomotion.Carrying = false;
            }

            return item;
        }
    }
}
