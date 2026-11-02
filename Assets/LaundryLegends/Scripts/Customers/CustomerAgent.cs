using System;
using LaundryLegends.Audio;
using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using UnityEngine;

namespace LaundryLegends.Customers
{
    public enum CustomerState
    {
        Arrive,
        Queue,
        HandOff,
        Waiting,
        Collect,
        Pay,
        Leave,
        Done
    }

    /// <summary>
    /// One customer's order lifecycle: walks in, hands laundry over, waits for
    /// it to be processed, collects, pays exactly once via the ledger, and
    /// leaves. Movement and carrying reuse the NPC components.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CustomerAgent : MonoBehaviour
    {
        [SerializeField] private float _walkSpeed = 1.1f;
        [SerializeField] private float _queueSeconds = 0.6f;
        [SerializeField] private float _pickupSeconds = 0.5f;
        [SerializeField] private int _price = 40;

        [Tooltip("Seconds a customer waits for the laundry before giving up. A full wash/dry/fold cycle takes about 75 s, so this leaves room for a player using the same machines.")]
        [SerializeField] private float _patienceSeconds = 600f;

        [Tooltip("One walk leg may not run longer than this. A blocked path skips the leg instead of freezing the order loop.")]
        [SerializeField] private float _walkTimeoutSeconds = 40f;

        [SerializeField] private int _tip = 10;
        [SerializeField] private float _tipWindowSeconds = 120f;

        public int LastTip { get; private set; }

        /// <summary>Seconds this customer will wait for the laundry before abandoning.</summary>
        public float PatienceSeconds => _patienceSeconds;

        private OrderLedger _ledger;
        private CarrySlot _orderSlot;
        private Transform _doorPoint;
        private Transform _counterPoint;
        private Transform _exitPoint;
        private NpcLocomotion _locomotion;
        private CarrySocket _socket;
        private DialogueVoicePlayer _voice;
        private LaundryItem _item;
        private LaundryOrder _order;
        private FoldingStation _folding;
        private bool _walkReached;

        // The room center and the doorway are clear of the counter, the door
        // leaves, and the player spawn. Straight lines between the entrance and
        // the counter graze furniture, and overlap correction can pin an NPC in
        // the collider shell, so every route passes through these points.
        private static readonly Vector3 InteriorWaypoint = new Vector3(0f, 0f, -0.6f);
        private static readonly Vector3 DoorwayWaypoint = new Vector3(1f, 0f, -4.3f);

        public CustomerState State { get; private set; } = CustomerState.Arrive;

        public LaundryOrder Order => _order;

        public LaundryItem Item => _item;

        /// <summary>The counter slot assigned to this customer for drop-off and pickup.</summary>
        public CarrySlot OrderSlot => _orderSlot;

        public event Action<CustomerAgent, CustomerState> StateEntered;

        public void Configure(
            OrderLedger ledger,
            CarrySlot orderSlot,
            Transform doorPoint,
            Transform counterPoint,
            Transform exitPoint,
            LaundryItem item)
        {
            _ledger = ledger;
            _orderSlot = orderSlot;
            _doorPoint = doorPoint;
            _counterPoint = counterPoint;
            _exitPoint = exitPoint;
            _item = item;
        }

        public void SetWalkSpeed(float speed)
        {
            _walkSpeed = Mathf.Max(0.2f, speed);
        }

        public void SetPatience(float seconds)
        {
            _patienceSeconds = Mathf.Max(0.5f, seconds);
        }

        private void Awake()
        {
            _locomotion = GetComponent<NpcLocomotion>();
            _socket = GetComponent<CarrySocket>();
            _voice = GetComponent<DialogueVoicePlayer>();
            _folding = UnityEngine.Object.FindFirstObjectByType<FoldingStation>();
        }

        private IEnumerator Start()
        {
            yield return RunOrder();

            // The spawner only clears its active customer and respawns when it sees
            // Done. Every exit path above therefore has to end here, otherwise one
            // interrupted order freezes the whole shop.
            Enter(CustomerState.Done);
        }

        /// <summary>
        /// The order itself. It may finish early for any number of reasons; the
        /// caller guarantees the Done state afterwards.
        /// </summary>
        private IEnumerator RunOrder()
        {
            Enter(CustomerState.Arrive);

            if (_item != null && _socket != null)
            {
                _socket.Attach(_item);
            }

            Speak(DialogueTrigger.Enter);
            yield return WalkTo(_doorPoint != null ? _doorPoint.position : transform.position);
            yield return WalkTo(InteriorWaypoint);
            yield return WalkTo(_counterPoint != null ? _counterPoint.position : transform.position);

            if (!_walkReached)
            {
                Debug.LogWarning($"[ORDER] {name} could not reach the counter; leaving without a ticket.");
                yield return LeaveAndFinish();
                yield break;
            }

            Enter(CustomerState.Queue);
            yield return new WaitForSeconds(_queueSeconds);

            Enter(CustomerState.HandOff);
            float handoffWait = 0f;
            while (_item != null && _orderSlot != null && !_orderSlot.TryPlace(_item))
            {
                if (handoffWait >= 8f)
                {
                    break;
                }

                handoffWait += Time.deltaTime;
                yield return null;
            }

            if (_orderSlot != null && _orderSlot.Occupant == _item)
            {
                _socket?.Detach();
                _item.transform.SetParent(_orderSlot.transform, false);
                if (!_ledger.TryCreateOrder(_item, _price, Time.time, out _order) || _order == null)
                {
                    Debug.LogWarning($"[ORDER] Could not open a ticket: {_ledger.LastResult}");
                    yield return LeaveAndFinish();
                    yield break;
                }

                if (!_ledger.TryAcceptOrder(_order.OrderId, Time.time))
                {
                    Debug.LogWarning($"[ORDER] Could not accept {_order.OrderId}: {_ledger.LastResult}");
                    yield return LeaveAndFinish();
                    yield break;
                }

                Debug.Log($"[ORDER] {_order.OrderId} accepted at {_price}.");
                Speak(DialogueTrigger.Counter);
            }
            else
            {
                // The drop-off stayed busy: leave instead of standing there forever.
                Debug.LogWarning("[ORDER] The drop-off slot stayed busy; the customer left without a ticket.");
                yield return LeaveAndFinish();
                yield break;
            }

            Enter(CustomerState.Waiting);
            bool abandoned = false;
            float waited = 0f;
            while (true)
            {
                if (_order == null || _item == null)
                {
                    Debug.LogWarning("[ORDER] The order or its laundry disappeared while waiting.");
                    yield return LeaveAndFinish();
                    yield break;
                }

                waited += Time.deltaTime;
                if (waited >= _patienceSeconds)
                {
                    _ledger.TryAbandon(_order.OrderId, Time.time);
                    abandoned = true;
                    break;
                }

                LaundryStage stage = _item.Stage;
                if (stage == LaundryStage.Washed || stage == LaundryStage.Dried)
                {
                    if (_order.State == OrderState.Accepted)
                    {
                        _ledger.TryMarkProcessing(_order.OrderId, Time.time);
                    }
                }
                else if (stage == LaundryStage.Wrapped)
                {
                    if (_order.State == OrderState.Processing || _order.State == OrderState.Accepted)
                    {
                        _ledger.TryMarkProcessing(_order.OrderId, Time.time);
                    }

                    if (_ledger.TryMarkReady(_order.OrderId, Time.time))
                    {
                        break;
                    }
                }

                yield return null;
            }

            if (abandoned)
            {
                Debug.LogWarning($"[ORDER] {_order.OrderId} abandoned after {waited:0}s without payment.");
                CleanUpAbandonedItem();
                yield return LeaveAndFinish();
                yield break;
            }

            Enter(CustomerState.Collect);
            yield return new WaitForSeconds(_pickupSeconds);

            if (_folding != null)
            {
                Vector3 towardRoom = (Vector3.zero - _folding.transform.position);
                towardRoom.y = 0f;
                Vector3 stand = _folding.transform.position
                                + (towardRoom.sqrMagnitude > 0.01f ? towardRoom.normalized : Vector3.back) * 2f;
                stand.y = transform.position.y;
                yield return WalkTo(stand);
            }

            LaundryItem collected = _folding != null ? _folding.TakeWrappedItem() : null;
            if (collected == null && _orderSlot != null)
            {
                collected = _orderSlot.Take();
            }

            if (collected == null)
            {
                collected = _item;
            }

            if (collected == null)
            {
                Debug.LogWarning($"[ORDER] {_order.OrderId} has no laundry to collect; leaving unpaid.");
                yield return LeaveAndFinish();
                yield break;
            }

            if (!_ledger.TryCollect(_order.OrderId, Time.time))
            {
                Debug.LogWarning($"[ORDER] Could not collect {_order.OrderId}: {_ledger.LastResult}");
                yield return LeaveAndFinish();
                yield break;
            }

            if (_socket != null)
            {
                _socket.Attach(collected);
            }

            Enter(CustomerState.Pay);
            if (_ledger.TryPay(_order.OrderId, Time.time))
            {
                if (waited <= _tipWindowSeconds && _tip > 0)
                {
                    LastTip = _ledger.TryAddTip(_order.OrderId, _tip, Time.time) ? _tip : 0;
                }

                Debug.Log($"[ORDER] {_order.OrderId} paid {_order.Price}; wallet {_ledger.Wallet.Money}.");
                GameAudio.Play(Sfx.Cash);
                Speak(DialogueTrigger.Pickup);
            }
            else
            {
                Debug.LogError($"[ORDER] Payment failed for {_order.OrderId}: {_ledger.LastResult}");
            }

            yield return new WaitForSeconds(_pickupSeconds);

            Enter(CustomerState.Leave);
            if (collected != null)
            {
                collected.gameObject.SetActive(false);
            }

            yield return WalkOut();
        }

        /// <summary>Walks out and returns, so the caller can exit the order cleanly.</summary>
        private IEnumerator LeaveAndFinish()
        {
            Enter(CustomerState.Leave);
            yield return WalkOut();
        }

        /// <summary>
        /// Exit route: through the room center, beside the player spawn, and out
        /// of the doorway. A direct line from the folding table or the counter
        /// clips a door leaf and can stall the NPC against it.
        /// </summary>
        private IEnumerator WalkOut()
        {
            yield return WalkTo(InteriorWaypoint);
            yield return WalkTo(DoorwayWaypoint);
            yield return WalkTo(_exitPoint != null ? _exitPoint.position : transform.position);
        }

        private IEnumerator WalkTo(Vector3 destination)
        {
            _walkReached = false;
            float elapsed = 0f;
            float limit = Mathf.Max(1f, _walkTimeoutSeconds);

            if (_locomotion != null)
            {
                while (!_locomotion.MoveTowards(destination, _walkSpeed, 0.15f))
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= limit)
                    {
                        Debug.LogWarning($"[ORDER] Walking to {destination} timed out after {limit:0}s.");
                        yield break;
                    }

                    yield return null;
                }

                _walkReached = true;
                yield break;
            }

            while (Vector3.Distance(transform.position, destination) > 0.15f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, _walkSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                if (elapsed >= limit)
                {
                    Debug.LogWarning($"[ORDER] Walking to {destination} timed out after {limit:0}s.");
                    yield break;
                }

                yield return null;
            }

            _walkReached = true;
        }

        /// <summary>
        /// Removes an abandoned customer's laundry from every stage. Without
        /// this the folding station stays occupied and the next order could
        /// never be wrapped.
        /// </summary>
        private void CleanUpAbandonedItem()
        {
            if (_item == null)
            {
                return;
            }

            if (_folding != null && _folding.ReadyItem == _item)
            {
                _folding.TakeWrappedItem();
            }

            if (_orderSlot != null && _orderSlot.Occupant == _item)
            {
                _orderSlot.Take();
            }

            foreach (MachineLoadHook machine in UnityEngine.Object.FindObjectsByType<MachineLoadHook>(
                         FindObjectsSortMode.None))
            {
                if (machine.LoadedItem == _item)
                {
                    machine.ForceUnload();
                }
            }

            if (_socket != null && _socket.IsCarrying && _socket.Carried == _item)
            {
                _socket.Detach();
            }

            // The staff may already be carrying it; detach from any socket so the
            // abandoned item cannot re-enter the pipeline.
            foreach (CarrySocket socket in UnityEngine.Object.FindObjectsByType<CarrySocket>(
                         FindObjectsSortMode.None))
            {
                if (socket.Carried == _item)
                {
                    socket.Detach();
                }
            }

            Destroy(_item.gameObject, 0.1f);
        }

        private void Speak(DialogueTrigger trigger)        {
            if (_voice != null)
            {
                _voice.Speak(trigger);
            }
        }

        private void Enter(CustomerState next)
        {
            State = next;
            StateEntered?.Invoke(this, next);
        }
    }
}
