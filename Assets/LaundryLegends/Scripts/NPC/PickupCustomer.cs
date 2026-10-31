using System.Collections;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Walks to a stand point outside the folding table when a wrapped bundle is
    /// ready, picks that bundle up into a CarrySocket (or into its arms when no
    /// socket is authored), walks back out, and only then destroys the bundle.
    /// The item never teleports: it is transferred once the customer arrives.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupCustomer : MonoBehaviour
    {
        [SerializeField] private float _speed = 1.2f;
        [SerializeField] private Vector3 _tableStand = new Vector3(2.55f, 0f, 1f);
        [SerializeField] private float _standDistanceFromTable = 1.95f;
        [SerializeField] private Vector3 _exitPoint = new Vector3(0f, 0f, -10.5f);
        [SerializeField] private Vector3 _carryLocalOffset = new Vector3(0.3f, 1.05f, 0.2f);
        [SerializeField] private float _arriveDistance = 0.08f;

        private FoldingStation _folding;
        private NpcLocomotion _locomotion;
        private CarrySocket _socket;
        private LaundryItem _bundle;

        private void Awake()
        {
            _locomotion = GetComponent<NpcLocomotion>();
            _socket = GetComponent<CarrySocket>();
        }

        private IEnumerator Start()
        {
            if (_locomotion == null)
            {
                _locomotion = GetComponent<NpcLocomotion>();
            }

            if (_socket == null)
            {
                _socket = GetComponent<CarrySocket>();
            }

            _folding = Object.FindFirstObjectByType<FoldingStation>();
            while (_folding == null)
            {
                yield return null;
                _folding = Object.FindFirstObjectByType<FoldingStation>();
            }

            yield return new WaitUntil(() => _folding != null && _folding.IsReady);

            yield return MoveTo(ResolveStandPoint());
            yield return TakeBundle();
            yield return MoveTo(_exitPoint);
            ClearBundle();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Serialized stand point, pushed to the room side of the table when it
        /// would otherwise sit inside the table footprint.
        /// </summary>
        private Vector3 ResolveStandPoint()
        {
            Vector3 point = _tableStand;
            if (_folding != null)
            {
                Vector3 table = _folding.transform.position;
                float outsideX = table.x - Mathf.Max(0.5f, _standDistanceFromTable);
                if (point.x > outsideX)
                {
                    point.x = outsideX;
                }
            }

            point.y = transform.position.y;
            return point;
        }

        private IEnumerator TakeBundle()
        {
            if (_folding == null || !_folding.IsReady)
            {
                yield break;
            }

            LaundryItem item = _folding.TakeWrappedItem();
            if (item == null)
            {
                yield break;
            }

            _bundle = item;
            if (_socket != null && _socket.Attach(item))
            {
                yield return new WaitForSeconds(0.2f);
                yield break;
            }

            item.transform.SetParent(transform, false);
            item.transform.localPosition = _carryLocalOffset;
            item.transform.localRotation = Quaternion.identity;
            foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            yield return new WaitForSeconds(0.2f);
        }

        private void ClearBundle()
        {
            if (_socket != null && _socket.IsCarrying)
            {
                LaundryItem detached = _socket.Detach();
                _bundle = null;
                if (detached != null)
                {
                    Destroy(detached.gameObject);
                }

                return;
            }

            if (_bundle != null)
            {
                Destroy(_bundle.gameObject);
            }

            _bundle = null;
        }

        private IEnumerator MoveTo(Vector3 destination)
        {
            if (_locomotion != null && _locomotion.HasAnimator)
            {
                while (!_locomotion.MoveTowards(destination, _speed, _arriveDistance))
                {
                    yield return null;
                }

                yield break;
            }

            while (Vector3.Distance(transform.position, destination) > _arriveDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, _speed * Time.deltaTime);
                Vector3 direction = destination - transform.position;
                if (new Vector2(direction.x, direction.z).sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                }

                yield return null;
            }
        }
    }
}
