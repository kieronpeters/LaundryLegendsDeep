using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>Which minigame a cabinet launches.</summary>
    public enum ArcadeGameKind
    {
        Fight,
        Maze
    }

    /// <summary>
    /// One purchasable arcade machine. Definitions are data only; the service
    /// owns the slots, money and scene instances.
    /// </summary>
    [CreateAssetMenu(menuName = "Laundry Legends/Arcade Machine", fileName = "ArcadeMachine")]
    public sealed class ArcadeMachineDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "arcade.fighter";
        [SerializeField] private string _displayName = "Fighter";
        [SerializeField, Min(0)] private int _cost = 120;
        [SerializeField, Min(1)] private int _baseVisitIncome = 3;
        [SerializeField, Min(0.1f)] private float _attractWeight = 1f;
        [SerializeField] private Vector2 _footprint = new Vector2(0.9f, 0.9f);
        [SerializeField] private GameObject _prefab;
        [SerializeField] private ArcadeGameKind _gameKind = ArcadeGameKind.Fight;
        [SerializeField] private string _opponentName = "BULL";
        [SerializeField] private Color _accentColor = new Color(0.38f, 0.78f, 1f);
        [SerializeField] private Color _opponentColor = new Color(1f, 0.47f, 0.38f);

        public string Id => _id;
        public string DisplayName => _displayName;
        public int Cost => _cost;
        public int BaseVisitIncome => _baseVisitIncome;
        public float AttractWeight => _attractWeight;
        public Vector2 Footprint => _footprint;
        public ArcadeGameKind GameKind => _gameKind;
        public string OpponentName => _opponentName;
        public Color AccentColor => _accentColor;
        public Color OpponentColor => _opponentColor;

        /// <summary>Optional cabinet visual. When null the slot uses its authored cabinet.</summary>
        public GameObject Prefab => _prefab;

        public void Configure(string id, string displayName, int cost, int baseVisitIncome, float attractWeight)
        {
            Configure(id, displayName, cost, baseVisitIncome, attractWeight, null);
        }

        public void Configure(
            string id,
            string displayName,
            int cost,
            int baseVisitIncome,
            float attractWeight,
            GameObject prefab)
        {
            _id = id;
            _displayName = displayName;
            _cost = Mathf.Max(0, cost);
            _baseVisitIncome = Mathf.Max(1, baseVisitIncome);
            _attractWeight = Mathf.Max(0.1f, attractWeight);
            _prefab = prefab;
        }

        /// <summary>Sets what the cabinet plays and how its screen is themed.</summary>
        public void ConfigureGame(
            ArcadeGameKind gameKind,
            string opponentName,
            Color accentColor,
            Color opponentColor)
        {
            _gameKind = gameKind;
            _opponentName = string.IsNullOrWhiteSpace(opponentName) ? "BULL" : opponentName;
            _accentColor = accentColor;
            _opponentColor = opponentColor;
        }

        /// <summary>Placeholder balance used when the scene has no authored assets.</summary>
        public static List<ArcadeMachineDefinition> CreateDefaultCatalogue()
        {
            return new List<ArcadeMachineDefinition>
            {
                Create("arcade.fighter", "Fighter", 120, 3, 1.0f),
                Create("arcade.racer", "Racer", 250, 5, 1.4f),
                Create("arcade.puzzle", "Puzzle", 400, 8, 1.8f)
            };
        }

        private static ArcadeMachineDefinition Create(string id, string name, int cost, int income, float weight)
        {
            var definition = CreateInstance<ArcadeMachineDefinition>();
            definition.Configure(id, name, cost, income, weight);
            definition.name = name;
            return definition;
        }
    }
}
