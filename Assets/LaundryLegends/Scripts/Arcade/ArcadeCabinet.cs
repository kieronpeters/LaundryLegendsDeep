using System;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// The arcade cabinet: charges one play through the economy service, runs the
    /// maze game, and reads movement input while a game is active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeCabinet : MonoBehaviour, IInteractable
    {
        [SerializeField] private MazeGameView _view;
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField] private int _cost = 2;
        [SerializeField] private float _endDelaySeconds = 2f;
        [SerializeField] private string _prompt = "Play maze game";

        private MazeGame _game;
        private float _endTimer;

        public bool IsPlaying => _game != null && !_game.IsOver;

        public int PlaysStarted { get; private set; }

        public MazeGame Game => _game;

        public int Cost => _cost;

        public void Configure(MazeGameView view, CustomerSpawner spawner)
        {
            _view = view;
            _spawner = spawner;
        }

        public string InteractionPrompt => IsPlaying ? "Stop game" : $"{_prompt} (${_cost})";

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled;
        }

        public void Interact(GameObject interactor)
        {
            if (IsPlaying)
            {
                Stop();
                return;
            }

            EconomyService economy = _spawner != null ? _spawner.Economy : null;
            TryStart(economy, Time.time);
        }

        /// <summary>
        /// Starts a paid play. Returns false when the play cannot be afforded; in
        /// that case nothing is charged and no game starts.
        /// </summary>
        public bool TryStart(EconomyService economy, float atSeconds)
        {
            if (IsPlaying || economy == null)
            {
                return false;
            }

            if (!economy.TrySpend(_cost, "Arcade play", atSeconds))
            {
                return false;
            }

            PlaysStarted += 1;
            _endTimer = 0f;
            _game = new MazeGame(new MazeLayout());
            if (_view != null)
            {
                _view.Show(_game);
            }

            return true;
        }

        public void Stop()
        {
            _game = null;
            _endTimer = 0f;
        }

        private void Update()
        {
            if (_game == null)
            {
                return;
            }

            if (_game.IsOver)
            {
                _endTimer += Time.deltaTime;
                if (_endTimer >= _endDelaySeconds)
                {
                    Stop();
                }

                return;
            }

            _game.Tick(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                _game.TryMovePlayer(Vector2Int.up);
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                _game.TryMovePlayer(Vector2Int.down);
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _game.TryMovePlayer(Vector2Int.left);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                _game.TryMovePlayer(Vector2Int.right);
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
            {
                Stop();
            }
        }
    }
}
