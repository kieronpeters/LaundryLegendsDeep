using System;
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

        public CustomerState State { get; private set; } = CustomerState.Arrive;

        public LaundryOrder Order => _order;

        public LaundryItem Item => _item;

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

        private void Awake()
        {
            _locomotion = GetComponent<NpcLocomotion>();
            _socket = GetComponent<CarrySocket>();
            _voice = GetComponent<DialogueVoicePlayer>();
        }

        private IEnumerator Start()
        {
            Enter(CustomerState.Arrive);

            if (_item != null && _socket != null)
            {
                _socket.Attach(_item);
            }

            Speak(DialogueTrigger.Enter);
            yield return WalkTo(_doorPoint != null ? _doorPoint.position : transform.position);
            yield return WalkTo(_counterPoint != null ? _counterPoint.position : transform.position);

            Enter(CustomerState.Queue);
            yield return new WaitForSeconds(_queueSeconds);

            Enter(CustomerState.HandOff);
            if (_item != null && _orderSlot != null && _orderSlot.TryPlace(_item))
            {
                _socket?.Detach();
                _item.transform.SetParent(_orderSlot.transform, false);
                _ledger.TryCreateOrder(_item, _price, Time.time, out _order);
                if (_order != null)
                {
                    _ledger.TryAcceptOrder(_order.OrderId, Time.time);
                }

                Speak(DialogueTrigger.Counter);
            }

            Enter(CustomerState.Waiting);
            while (true)
            {
                if (_order == null || _item == null)
                {
                    yield break;
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

            Enter(CustomerState.Collect);
            yield return new WaitForSeconds(_pickupSeconds);

            LaundryItem collected = _orderSlot != null ? _orderSlot.Take() : _item;
            if (collected != null)
            {
                _ledger.TryCollect(_order.OrderId, Time.time);
                if (_socket != null)
                {
                    _socket.Attach(collected);
                }
            }

            Enter(CustomerState.Pay);
            if (_ledger.TryPay(_order.OrderId, Time.time))
            {
                Speak(DialogueTrigger.Pickup);
            }

            yield return new WaitForSeconds(_pickupSeconds);

            Enter(CustomerState.Leave);
            if (collected != null)
            {
                collected.gameObject.SetActive(false);
            }

            yield return WalkTo(_exitPoint != null ? _exitPoint.position : transform.position);
            Enter(CustomerState.Done);
        }

        private IEnumerator WalkTo(Vector3 destination)
        {
            if (_locomotion != null)
            {
                while (!_locomotion.MoveTowards(destination, _walkSpeed, 0.15f))
                {
                    yield return null;
                }

                yield break;
            }

            while (Vector3.Distance(transform.position, destination) > 0.15f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, _walkSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private void Speak(DialogueTrigger trigger)
        {
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
