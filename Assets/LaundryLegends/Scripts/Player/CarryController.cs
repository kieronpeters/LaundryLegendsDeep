using System;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.Player
{
    /// <summary>
    /// Holds at most one carryable laundry item. Carrying is attachment-based:
    /// the item is parented to an explicit hold anchor and its world physics is
    /// suspended until it is dropped or placed into a compatible slot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarryController : MonoBehaviour
    {
        [SerializeField] private Transform _holdAnchor;
        [SerializeField] private float _dropDistance = 1.2f;
        [SerializeField] private LayerMask _dropBlockMask = ~0;
        [SerializeField] private KeyCode _dropKey = KeyCode.Q;

        [NonSerialized] private Rigidbody _heldBody;
        [NonSerialized] private bool _heldWasKinematic;
        [NonSerialized] private bool _heldUsedGravity;
        [NonSerialized] private Collider[] _heldColliders = Array.Empty<Collider>();
        [NonSerialized] private bool[] _heldColliderEnabled = Array.Empty<bool>();

        public LaundryItem HeldItem { get; private set; }

        public bool IsCarrying => HeldItem != null;

        public Transform HoldAnchor => _holdAnchor != null ? _holdAnchor : transform;

        public bool HasHoldAnchor => _holdAnchor != null;

        /// <summary>Serialized drop input; the prompt view reads its label.</summary>
        public KeyCode DropKey => _dropKey;

        public string DropKeyLabel => _dropKey.ToString();

        private void Update()
        {
            if (HeldItem == null)
            {
                ResetHeldState();
                return;
            }

            if (Time.timeScale > 0f && Input.GetKeyDown(_dropKey))
            {
                TryDrop();
                return;
            }

            if (!HeldItem.gameObject.activeInHierarchy)
            {
                TryDrop();
            }
        }

        private void OnDisable()
        {
            if (HeldItem != null && gameObject.activeInHierarchy && gameObject.scene.isLoaded)
            {
                TryDrop();
            }
            else
            {
                ResetHeldState();
            }
        }

        public bool TryPickUp(LaundryItem item)
        {
            if (HeldItem != null)
            {
                return false;
            }

            if (item == null || !item.IsCarryable || !item.gameObject.activeInHierarchy)
            {
                return false;
            }

            HeldItem = item;
            Attach(item);
            return true;
        }

        public bool TryDrop()
        {
            if (HeldItem == null)
            {
                return false;
            }

            LaundryItem item = HeldItem;
            Vector3 position = ResolveDropPosition();
            Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            Detach(item);
            item.transform.SetPositionAndRotation(position, rotation);
            ResetHeldState();
            return true;
        }

        public bool TryPlaceInto(CarrySlot slot)
        {
            if (slot == null || HeldItem == null)
            {
                return false;
            }

            LaundryItem item = HeldItem;
            if (!slot.CanAccept(item))
            {
                return false;
            }

            Detach(item);
            bool placed = slot.TryPlace(item);
            ResetHeldState();
            return placed;
        }

        private void Attach(LaundryItem item)
        {
            Transform anchor = HoldAnchor;
            item.transform.SetParent(anchor, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            _heldBody = item.GetComponent<Rigidbody>();
            if (_heldBody != null)
            {
                _heldWasKinematic = _heldBody.isKinematic;
                _heldUsedGravity = _heldBody.useGravity;
                if (!_heldBody.isKinematic)
                {
                    _heldBody.linearVelocity = Vector3.zero;
                    _heldBody.angularVelocity = Vector3.zero;
                }

                _heldBody.isKinematic = true;
                _heldBody.useGravity = false;
            }

            _heldColliders = item.GetComponentsInChildren<Collider>(true);
            _heldColliderEnabled = new bool[_heldColliders.Length];
            for (int i = 0; i < _heldColliders.Length; i++)
            {
                _heldColliderEnabled[i] = _heldColliders[i].enabled;
                _heldColliders[i].enabled = false;
            }
        }

        private void Detach(LaundryItem item)
        {
            item.transform.SetParent(null, true);
            RestoreWorldState();
        }

        private void RestoreWorldState()
        {
            if (_heldBody != null)
            {
                _heldBody.isKinematic = _heldWasKinematic;
                _heldBody.useGravity = _heldUsedGravity;
            }

            for (int i = 0; i < _heldColliders.Length; i++)
            {
                if (_heldColliders[i] != null)
                {
                    _heldColliders[i].enabled = _heldColliderEnabled[i];
                }
            }

            _heldBody = null;
            _heldColliders = Array.Empty<Collider>();
            _heldColliderEnabled = Array.Empty<bool>();
        }

        private Vector3 ResolveDropPosition()
        {
            Vector3 direction = transform.forward;
            float distance = _dropDistance;
            Vector3 origin = transform.position + Vector3.up * 0.8f;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, _dropDistance + 0.2f, _dropBlockMask, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Clamp(hit.distance - 0.3f, 0.2f, _dropDistance);
            }

            Vector3 position = transform.position + direction * distance;
            position.y = transform.position.y + 0.1f;
            return position;
        }

        private void ResetHeldState()
        {
            HeldItem = null;
            _heldBody = null;
            _heldColliders = Array.Empty<Collider>();
            _heldColliderEnabled = Array.Empty<bool>();
        }
    }
}
