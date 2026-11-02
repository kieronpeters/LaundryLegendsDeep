using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Staff loop with visible carrying: walks to the counter, picks up the
    /// customer's laundry, carries it to the washer, then the dryer, then the
    /// folding table, and wraps it for pickup. The wash/dry timers stay owned by
    /// <see cref="MachineLoadHook"/>; this component only moves and transfers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LaundryStaff : MonoBehaviour
    {
        [SerializeField] private float _walkSpeed = 1.1f;
        [SerializeField] private float _standDistance = 0.85f;
        [SerializeField] private float _foldStandDistance = 2f;

        [Tooltip("Distance from the drop-off slot to the staff's stand point. The slot sits on the counter, so this must clear the counter's collider or overlap correction pins the staff short of it.")]
        [SerializeField] private float _dropOffStandDistance = 1.6f;
        [SerializeField] private float _carryLiftSeconds = 0.45f;
        [SerializeField] private Vector3 _idlePoint = new Vector3(-4.6f, 0f, -0.9f);

        private MachineLoadHook _washer;
        private MachineLoadHook _dryer;
        private FoldingStation _folding;
        private CarrySlot _dropOff;
        private NpcLocomotion _locomotion;
        private CarrySocket _socket;

        // Only used when the prefab has no CarrySocket: the item still travels
        // with the staff member instead of jumping between stations.
        private LaundryItem _held;

        public float WalkSpeed => _walkSpeed;

        public LaundryItem HeldItem =>
            _socket != null && _socket.IsCarrying ? _socket.Carried : _held;

        public bool IsCarrying => HeldItem != null;

        /// <summary>Test/authoring hook to speed up the walk between stations.</summary>
        public void SetWalkSpeed(float speed)
        {
            _walkSpeed = Mathf.Max(0.05f, speed);
        }

        private void Awake()
        {
            _locomotion = GetComponent<NpcLocomotion>();
            _socket = GetComponent<CarrySocket>();
        }

        private IEnumerator Start()
        {
            _washer = FindMachine(MachineKind.Washer, true);
            _dryer = FindMachine(MachineKind.Dryer, true);
            _folding = Object.FindFirstObjectByType<FoldingStation>();
            _dropOff = FindDropOff();

            while (true)
            {
                yield return WaitForWork();
                yield return WalkTo(StandPoint(_dropOff.transform, _dropOffStandDistance));
                if (!TakeFromSlot(_dropOff))
                {
                    yield return null;
                    continue;
                }

                yield return LoadInto(_washer);
                if (HeldItem != null)
                {
                    PutHeldBack();
                    yield return null;
                    continue;
                }

                yield return WalkTo(_idlePoint);
                yield return WaitForComplete(_washer);
                if (_washer == null)
                {
                    continue;
                }

                yield return WalkTo(StandPoint(_washer.transform, _standDistance));
                if (!TakeFromMachine(_washer))
                {
                    yield return null;
                    continue;
                }

                yield return WaitForDryer();
                if (_dryer == null)
                {
                    PutHeldBack();
                    yield return null;
                    continue;
                }

                yield return WalkTo(StandPoint(_dryer.transform, _standDistance));
                yield return LoadInto(_dryer);
                if (HeldItem != null)
                {
                    PutHeldBack();
                    yield return null;
                    continue;
                }

                yield return WalkTo(_idlePoint);
                yield return WaitForComplete(_dryer);
                if (_dryer == null)
                {
                    continue;
                }

                yield return WalkTo(StandPoint(_dryer.transform, _standDistance));
                if (!TakeFromMachine(_dryer))
                {
                    yield return null;
                    continue;
                }

                yield return WaitForFolding();
                if (_folding == null)
                {
                    PutHeldBack();
                    yield return null;
                    continue;
                }

                yield return WalkTo(StandPoint(_folding.transform, _foldStandDistance));
                yield return Wrap();

                yield return WalkTo(_idlePoint);
            }
        }

        /// <summary>
        /// World point on the room side of a station. Stations face the wall, so
        /// the approach direction is derived from the room centre instead of
        /// <see cref="Transform.forward"/>.
        /// </summary>
        private Vector3 StandPoint(Transform station, float distance)
        {
            if (station == null)
            {
                return transform.position;
            }

            Vector3 toRoom = Vector3.zero - station.position;
            Vector3 direction = new Vector3(toRoom.x, 0f, toRoom.z);
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.back;
            }

            direction.Normalize();
            Vector3 point = station.position + direction * Mathf.Max(0.1f, distance);
            point.y = transform.position.y;
            return point;
        }

        private IEnumerator WalkTo(Vector3 destination)
        {
            float elapsed = 0f;
            const float limit = 20f;

            if (_locomotion != null)
            {
                while (!_locomotion.MoveTowards(destination, _walkSpeed))
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= limit)
                    {
                        // A blocked stand point must not freeze the whole staff loop.
                        Debug.LogWarning($"[Staff] Walking to {destination} timed out after {limit:0}s; retrying the station.");
                        yield break;
                    }

                    yield return null;
                }

                yield break;
            }

            while (Vector3.Distance(transform.position, destination) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, _walkSpeed * Time.deltaTime);
                Vector3 direction = destination - transform.position;
                if (new Vector2(direction.x, direction.z).sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                }

                elapsed += Time.deltaTime;
                if (elapsed >= limit)
                {
                    Debug.LogWarning($"[Staff] Walking to {destination} timed out after {limit:0}s; retrying the station.");
                    yield break;
                }

                yield return null;
            }
        }

        private bool CanStartCycle()
        {
            return !IsCarrying
                && _dropOff != null
                && !_dropOff.IsEmpty
                && _washer != null
                && _washer.State == MachineState.Empty;
        }

        private IEnumerator WaitForWork()
        {
            while (!CanStartCycle())
            {
                if (_washer == null || _washer.State != MachineState.Empty)
                {
                    _washer = FindMachine(MachineKind.Washer, true);
                }

                if (_dropOff == null)
                {
                    _dropOff = FindDropOff();
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        private IEnumerator LoadInto(MachineLoadHook machine)
        {
            if (machine == null)
            {
                yield break;
            }

            yield return WalkTo(StandPoint(machine.transform, _standDistance));
            for (int attempt = 0; attempt < 10 && HeldItem != null; attempt++)
            {
                if (Load(machine))
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator WaitForComplete(MachineLoadHook machine)
        {
            if (machine == null)
            {
                yield break;
            }

            while (machine != null && machine.State != MachineState.Complete)
            {
                if (machine.State == MachineState.Empty)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator WaitForDryer()
        {
            bool warned = false;
            while (_dryer == null || _dryer.State != MachineState.Empty)
            {
                _dryer = FindMachine(MachineKind.Dryer, true);
                if (_dryer == null && !warned)
                {
                    warned = true;
                    Debug.LogWarning("[Staff] No free dryer found; waiting with the washed laundry in hand.");
                }

                yield return null;
            }
        }

        private IEnumerator WaitForFolding()
        {
            bool warned = false;
            while (_folding == null)
            {
                _folding = Object.FindFirstObjectByType<FoldingStation>();
                if (_folding == null && !warned)
                {
                    warned = true;
                    Debug.LogWarning("[Staff] No folding station found; waiting with the dried laundry in hand.");
                }

                yield return null;
            }
        }

        private bool TakeFromSlot(CarrySlot slot)
        {
            if (slot == null || IsCarrying)
            {
                return false;
            }

            LaundryItem item = slot.Take();
            if (item == null)
            {
                return false;
            }

            return Carry(item);
        }

        private bool TakeFromMachine(MachineLoadHook machine)
        {
            if (machine == null || IsCarrying)
            {
                return false;
            }

            LaundryItem item = machine.TakeProcessedItem();
            if (item == null)
            {
                return false;
            }

            return Carry(item);
        }

        private bool Load(MachineLoadHook machine)
        {
            if (machine == null || !IsCarrying || machine.State != MachineState.Empty)
            {
                return false;
            }

            LaundryItem item = ReleaseHeld();
            if (item == null)
            {
                return false;
            }

            if (!machine.LoadProcessedItem(item))
            {
                // Never lose the item: keep holding it and retry.
                Carry(item);
                return false;
            }

            if (!machine.StartCycle())
            {
                Debug.LogWarning($"[Staff] {machine.name} refused to start after a successful load.");
            }

            return true;
        }

        private IEnumerator Wrap()
        {
            LaundryItem item = ReleaseHeld();
            if (item == null)
            {
                yield break;
            }

            bool wrapped = _folding != null && _folding.WrapItem(item);
            if (!wrapped)
            {
                Carry(item);
            }

            yield return new WaitForSeconds(_carryLiftSeconds);
        }

        private bool Carry(LaundryItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (_socket != null && _socket.Attach(item))
            {
                return true;
            }

            // No carry socket (or a busy one): keep the item with the NPC so a
            // later transfer can still hand it to a machine or the table.
            _held = item;
            item.transform.SetParent(transform, true);
            if (_locomotion != null)
            {
                _locomotion.Carrying = true;
            }

            return true;
        }

        private LaundryItem ReleaseHeld()
        {
            if (_socket != null && _socket.IsCarrying)
            {
                return _socket.Detach();
            }

            LaundryItem item = _held;
            _held = null;
            if (item != null && _locomotion != null)
            {
                _locomotion.Carrying = false;
            }

            return item;
        }

        private void PutHeldBack()
        {
            LaundryItem item = ReleaseHeld();
            if (item == null)
            {
                return;
            }

            if (_dropOff == null)
            {
                _dropOff = FindDropOff();
            }

            if (_dropOff == null || !_dropOff.TryPlace(item))
            {
                // Last resort: keep carrying rather than deleting or losing it.
                Carry(item);
            }
        }

        private static MachineLoadHook FindMachine(MachineKind kind, bool requireEmpty)
        {
            MachineLoadHook[] hooks = Object.FindObjectsByType<MachineLoadHook>(FindObjectsSortMode.None);
            System.Array.Sort(hooks, (a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

            MachineLoadHook fallback = null;
            foreach (MachineLoadHook hook in hooks)
            {
                if (hook == null || !hook.isActiveAndEnabled || hook.Kind != kind)
                {
                    continue;
                }

                if (!requireEmpty || hook.State == MachineState.Empty)
                {
                    return hook;
                }

                if (fallback == null)
                {
                    fallback = hook;
                }
            }

            return requireEmpty ? null : fallback;
        }

        private static CarrySlot FindDropOff()
        {
            GameObject target = GameObject.Find("DropOffSlot");
            return target != null ? target.GetComponent<CarrySlot>() : null;
        }
    }
}
