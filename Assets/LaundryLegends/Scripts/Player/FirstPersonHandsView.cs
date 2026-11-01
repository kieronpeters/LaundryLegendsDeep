using UnityEngine;

namespace LaundryLegends.Player
{
    /// <summary>
    /// Shows the first-person hands only while the player is carrying something.
    /// Empty hands stay hidden, and a pickup reads as the hands taking the item
    /// rather than the item appearing in front of nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstPersonHandsView : MonoBehaviour
    {
        [SerializeField] private GameObject _hands;
        [SerializeField] private CarryController _carry;

        /// <summary>True while the hand meshes are on screen.</summary>
        public bool AreHandsVisible => _hands != null && _hands.activeSelf;

        public void Configure(GameObject hands, CarryController carry)
        {
            _hands = hands;
            _carry = carry;
            Apply();
        }

        private void Awake()
        {
            if (_carry == null)
            {
                _carry = GetComponent<CarryController>();
            }

            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (_hands == null)
            {
                return;
            }

            bool shouldShow = _carry != null && _carry.IsCarrying;
            if (_hands.activeSelf != shouldShow)
            {
                _hands.SetActive(shouldShow);
            }
        }
    }
}
