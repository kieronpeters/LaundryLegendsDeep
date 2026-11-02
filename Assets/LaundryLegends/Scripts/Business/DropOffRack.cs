using System.Collections.Generic;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.Business
{
    /// <summary>
    /// The counter rack that holds one order slot per active customer. Each slot
    /// is both the drop-off point and the ready-order shelf: the staff takes the
    /// item, processes it, and returns the wrapped parcel to the same slot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropOffRack : MonoBehaviour
    {
        [SerializeField] private List<CarrySlot> _slots = new List<CarrySlot>();

        private readonly HashSet<CarrySlot> _reserved = new HashSet<CarrySlot>();

        public IReadOnlyList<CarrySlot> Slots => _slots;

        public int Capacity => _slots.Count;

        public void Configure(IEnumerable<CarrySlot> slots)
        {
            _slots.Clear();
            _reserved.Clear();
            if (slots == null)
            {
                return;
            }

            foreach (CarrySlot slot in slots)
            {
                AddSlot(slot);
            }
        }

        public void AddSlot(CarrySlot slot)
        {
            if (slot != null && !_slots.Contains(slot))
            {
                _slots.Add(slot);
            }
        }

        public CarrySlot FindFreeSlot()
        {
            foreach (CarrySlot slot in _slots)
            {
                if (slot != null && slot.IsEmpty)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// Reserves an empty, unreserved slot for a customer that is still walking
        /// to the counter. The slot is released when that customer leaves.
        /// </summary>
        public CarrySlot ReserveFreeSlot()
        {
            foreach (CarrySlot slot in _slots)
            {
                if (slot != null && slot.IsEmpty && !_reserved.Contains(slot))
                {
                    _reserved.Add(slot);
                    return slot;
                }
            }

            return null;
        }

        public void Release(CarrySlot slot)
        {
            if (slot != null)
            {
                _reserved.Remove(slot);
            }
        }

        public bool IsReserved(CarrySlot slot)
        {
            return slot != null && _reserved.Contains(slot);
        }

        public bool TryFindOccupied(out CarrySlot slot)
        {
            foreach (CarrySlot candidate in _slots)
            {
                if (candidate != null && !candidate.IsEmpty)
                {
                    slot = candidate;
                    return true;
                }
            }

            slot = null;
            return false;
        }

        public int OccupiedCount
        {
            get
            {
                int count = 0;
                foreach (CarrySlot slot in _slots)
                {
                    if (slot != null && !slot.IsEmpty)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Wrapped parcels waiting for their customers.</summary>
        public int ReadyCount
        {
            get
            {
                int count = 0;
                foreach (CarrySlot slot in _slots)
                {
                    LaundryItem item = slot != null ? slot.Occupant : null;
                    if (item != null && item.Stage == LaundryStage.Wrapped)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
