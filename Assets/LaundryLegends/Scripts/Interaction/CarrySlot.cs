using System;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// One-item placement boundary. Accepts a compatible item only when empty,
    /// and keeps the occupant stable without adding inventory or load grouping.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarrySlot : MonoBehaviour
    {
        [SerializeField] private Transform _anchor;
        [SerializeField] private bool _acceptAllKinds = true;
        [SerializeField] private LaundryItemKind[] _acceptedKinds = Array.Empty<LaundryItemKind>();

        [NonSerialized] private LaundryItem _occupant;
        [NonSerialized] private Rigidbody _occupantBody;
        [NonSerialized] private bool _wasKinematic;
        [NonSerialized] private bool _usedGravity;

        public Transform Anchor => _anchor != null ? _anchor : transform;

        public LaundryItem Occupant => _occupant != null ? _occupant : null;

        public bool IsEmpty => _occupant == null;

        public bool CanAccept(LaundryItem item)
        {
            return item != null
                && _occupant == null
                && item.IsCarryable
                && IsKindAccepted(item.Kind);
        }

        /// <summary>Read-only kind check for editor validation and tests.</summary>
        public bool AcceptsKind(LaundryItemKind kind)
        {
            return IsKindAccepted(kind);
        }

        public void SetAcceptedKinds(params LaundryItemKind[] kinds)
        {
            _acceptedKinds = kinds ?? Array.Empty<LaundryItemKind>();
            _acceptAllKinds = false;
        }

        public void AcceptAllKinds()
        {
            _acceptedKinds = Array.Empty<LaundryItemKind>();
            _acceptAllKinds = true;
        }

        public bool TryPlace(LaundryItem item)
        {
            if (!CanAccept(item))
            {
                return false;
            }

            _occupant = item;
            item.transform.SetParent(Anchor, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            _occupantBody = item.GetComponent<Rigidbody>();
            if (_occupantBody != null)
            {
                _wasKinematic = _occupantBody.isKinematic;
                _usedGravity = _occupantBody.useGravity;
                if (!_occupantBody.isKinematic)
                {
                    _occupantBody.linearVelocity = Vector3.zero;
                    _occupantBody.angularVelocity = Vector3.zero;
                }

                _occupantBody.isKinematic = true;
                _occupantBody.useGravity = false;
            }

            return true;
        }

        /// <summary>Removes and returns the occupant, restoring its world physics state.</summary>
        public LaundryItem Take()
        {
            LaundryItem item = Occupant;
            if (item == null)
            {
                ResetOccupant();
                return null;
            }

            item.transform.SetParent(null, true);
            RestoreBody();
            ResetOccupant();
            return item;
        }

        /// <summary>Removes only the given occupant, leaving a mismatched occupant in place.</summary>
        public bool TryRemove(LaundryItem item)
        {
            if (item == null || Occupant != item)
            {
                return false;
            }

            Take();
            return true;
        }

        private bool IsKindAccepted(LaundryItemKind kind)
        {
            return _acceptAllKinds || Array.IndexOf(_acceptedKinds, kind) >= 0;
        }

        private void RestoreBody()
        {
            if (_occupantBody != null)
            {
                _occupantBody.isKinematic = _wasKinematic;
                _occupantBody.useGravity = _usedGravity;
            }

            _occupantBody = null;
        }

        private void ResetOccupant()
        {
            _occupant = null;
            _occupantBody = null;
            _wasKinematic = false;
            _usedGravity = false;
        }
    }
}
