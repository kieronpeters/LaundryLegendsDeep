using UnityEngine;

namespace LaundryLegends.Business
{
    /// <summary>The consumables the laundry consumes while processing an order.</summary>
    public enum ConsumableType
    {
        Detergent,
        Fragrance,
        Plastic
    }

    /// <summary>Data for one stocked consumable: name, unit price and shelf capacity.</summary>
    [CreateAssetMenu(menuName = "Laundry Legends/Consumable", fileName = "Consumable")]
    public sealed class ConsumableDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "consumable.detergent";
        [SerializeField] private string _displayName = "Detergent";
        [SerializeField] private ConsumableType _type = ConsumableType.Detergent;
        [SerializeField, Min(0)] private int _unitCost = 2;
        [SerializeField, Min(1)] private int _capacity = 10;

        public string Id => _id;
        public string DisplayName => _displayName;
        public ConsumableType Type => _type;
        public int UnitCost => _unitCost;
        public int Capacity => _capacity;

        public void Configure(
            string id,
            string displayName,
            ConsumableType type,
            int unitCost,
            int capacity)
        {
            _id = id;
            _displayName = displayName;
            _type = type;
            _unitCost = Mathf.Max(0, unitCost);
            _capacity = Mathf.Max(1, capacity);
        }
    }
}
