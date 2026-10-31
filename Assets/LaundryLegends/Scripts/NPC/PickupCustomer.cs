using System.Collections;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>Walks to the folding table when a wrapped bundle is ready and takes it away.</summary>
    public sealed class PickupCustomer : MonoBehaviour
    {
        [SerializeField] private float _speed = 1.2f;
        [SerializeField] private Vector3 _tableStand = new Vector3(3.4f, 0f, 1.0f);

        private FoldingStation _folding;

        private IEnumerator Start()
        {
            _folding = Object.FindFirstObjectByType<FoldingStation>();
            _lastPosition = transform.position;
            while (true)
            {
                yield return new WaitUntil(() => _folding != null && _folding.IsReady);
                yield return MoveTo(_tableStand);
                LaundryItem item = _folding.TakeWrappedItem();
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                    Destroy(item.gameObject, 0.2f);
                }

                yield return MoveTo(new Vector3(0f, 0f, -10.5f));
                gameObject.SetActive(false);
                yield break;
            }
        }

        private Vector3 _lastPosition;

        private IEnumerator MoveTo(Vector3 destination)
        {
            while (Vector3.Distance(transform.position, destination) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, _speed * Time.deltaTime);
                Vector3 direction = destination - transform.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }

                yield return null;
            }
        }
    }
}
