using UnityEngine;

namespace LaundryLegends.Core
{
    /// <summary>
    /// Owns the persistent <see cref="GameState"/> and survives scene loads.
    /// Created once by the Bootstrap scene; duplicate instances destroy themselves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private int _startingMoney = 100;

        /// <summary>
        /// The active session. This is a service reference, not gameplay state;
        /// money and progression remain inside <see cref="State"/>.
        /// </summary>
        public static GameSession Instance { get; private set; }

        public GameState State { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            State = new GameState(_startingMoney);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
