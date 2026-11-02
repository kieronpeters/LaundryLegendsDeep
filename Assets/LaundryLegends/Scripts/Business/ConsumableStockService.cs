using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Business
{
    /// <summary>
    /// Aggregates the shop's consumable shelves so machines and the folding station
    /// can ask for stock without knowing the scene layout. With no shelf for a type,
    /// consumption fails and the stage is blocked.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConsumableStockService : MonoBehaviour
    {
        [SerializeField] private List<ConsumableShelf> _shelves = new List<ConsumableShelf>();

        public event Action Changed;

        public IReadOnlyList<ConsumableShelf> Shelves => _shelves;

        public void Configure(IEnumerable<ConsumableShelf> shelves)
        {
            Unsubscribe();
            _shelves.Clear();
            if (shelves != null)
            {
                foreach (ConsumableShelf shelf in shelves)
                {
                    if (shelf != null)
                    {
                        _shelves.Add(shelf);
                    }
                }
            }

            Subscribe();
            Changed?.Invoke();
        }

        public void AddShelf(ConsumableShelf shelf)
        {
            if (shelf == null || _shelves.Contains(shelf))
            {
                return;
            }

            _shelves.Add(shelf);
            shelf.Changed += OnShelfChanged;
            Changed?.Invoke();
        }

        public ConsumableShelf Find(ConsumableType type)
        {
            EnsureShelves();
            foreach (ConsumableShelf shelf in _shelves)
            {
                if (shelf != null && shelf.Definition != null && shelf.Definition.Type == type)
                {
                    return shelf;
                }
            }

            return null;
        }

        public int Count(ConsumableType type)
        {
            ConsumableShelf shelf = Find(type);
            return shelf != null ? shelf.Count : 0;
        }

        public int Capacity(ConsumableType type)
        {
            ConsumableShelf shelf = Find(type);
            return shelf != null ? shelf.Capacity : 0;
        }

        public bool HasStock(ConsumableType type)
        {
            ConsumableShelf shelf = Find(type);
            return shelf != null && !shelf.IsEmpty;
        }

        /// <summary>Consumes one unit. Returns false when the type is missing or empty.</summary>
        public bool TryConsume(ConsumableType type)
        {
            ConsumableShelf shelf = Find(type);
            return shelf != null && shelf.TryConsumeOne();
        }

        public string DisplayName(ConsumableType type)
        {
            ConsumableShelf shelf = Find(type);
            return shelf != null && shelf.Definition != null ? shelf.Definition.DisplayName : type.ToString();
        }

        public string Describe(ConsumableType type)
        {
            ConsumableShelf shelf = Find(type);
            if (shelf == null)
            {
                return $"{type}: no shelf";
            }

            return $"{shelf.Definition.DisplayName} {shelf.Count}/{shelf.Capacity}";
        }

        private void Awake()
        {
            EnsureShelves();
        }

        private void EnsureShelves()
        {
            if (_shelves.Count > 0)
            {
                return;
            }

            foreach (ConsumableShelf shelf in FindObjectsByType<ConsumableShelf>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (shelf != null)
                {
                    _shelves.Add(shelf);
                    shelf.Changed += OnShelfChanged;
                }
            }
        }

        private void Subscribe()
        {
            foreach (ConsumableShelf shelf in _shelves)
            {
                if (shelf != null)
                {
                    shelf.Changed += OnShelfChanged;
                }
            }
        }

        private void Unsubscribe()
        {
            foreach (ConsumableShelf shelf in _shelves)
            {
                if (shelf != null)
                {
                    shelf.Changed -= OnShelfChanged;
                }
            }
        }

        private void OnShelfChanged(ConsumableShelf shelf)
        {
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
