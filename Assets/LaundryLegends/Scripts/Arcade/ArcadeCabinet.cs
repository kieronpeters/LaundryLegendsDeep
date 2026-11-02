using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// The arcade cabinet. One interaction charges a play and opens the cabinet's
    /// PLAY/QUIT menu; PLAY starts the fight, QUIT (or five seconds after the
    /// result) returns to the menu, and quitting hands the player back to the shop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeCabinet : MonoBehaviour, IInteractable
    {
        [SerializeField] private FightGameView _view;
        [SerializeField] private CustomerSpawner _spawner;
        [SerializeField] private ArcadeService _service;
        [SerializeField, Min(0)] private int _slotIndex;
        [SerializeField] private int _cost = 2;
        [SerializeField] private string _prompt = "Play arcade";

        private FightSession _session;
        private MazeGame _maze;
        private MazeGameView _mazeView;
        private float _mazeOverSeconds;
        [SerializeField] private ArcadeGameKind _gameKind = ArcadeGameKind.Fight;
        [SerializeField] private string _opponentName = "BULL";
        [SerializeField] private Color _accentColor = new Color(0.38f, 0.78f, 1f);
        [SerializeField] private Color _opponentColor = new Color(1f, 0.47f, 0.38f);

        public bool IsPlaying => _session != null || _maze != null;

        /// <summary>Which minigame this cabinet launches.</summary>
        public ArcadeGameKind GameKind => _gameKind;

        /// <summary>Active maze session, or null for fight cabinets.</summary>
        public MazeGame Maze => _maze;

        /// <summary>Opponent name shown on a fight cabinet.</summary>
        public string OpponentName => _opponentName;

        /// <summary>Applies the machine definition's game kind, names and colours.</summary>
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
            if (_view != null)
            {
                _view.SetTheme(accentColor, opponentColor);
            }
        }

        /// <summary>Which service slot powers this cabinet. Slot 0 is the original machine.</summary>
        public int SlotIndex => _slotIndex;

        /// <summary>
        /// False until the machine has been bought and activated. A cabinet with no
        /// service wired stays unlocked so isolated tests and prefab authoring work.
        /// </summary>
        public bool IsUnlocked => _service == null || _service.IsSlotActive(_slotIndex);

        public int PlaysStarted { get; private set; }

        public FightSession Session => _session;

        public int Cost => _cost;

        public void Configure(FightGameView view, CustomerSpawner spawner)
        {
            _view = view;
            _spawner = spawner;
        }

        /// <summary>Links the cabinet to the business loop that powers it (slot 0).</summary>
        public void SetService(ArcadeService service)
        {
            SetService(service, 0);
        }

        /// <summary>Links the cabinet to one slot of the business loop.</summary>
        public void SetService(ArcadeService service, int slotIndex)
        {
            _service = service;
            _slotIndex = Mathf.Max(0, slotIndex);
        }

        public string InteractionPrompt => IsPlaying
            ? "Arcade: playing"
            : IsUnlocked
                ? $"{_prompt} (${_cost})"
                : "Arcade machine (offline)";

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && IsUnlocked;
        }

        public void Interact(GameObject interactor)
        {
            if (!IsUnlocked)
            {
                return;
            }

            if (IsPlaying)
            {
                Stop();
                return;
            }

            EconomyService economy = _spawner != null ? _spawner.Economy : null;
            TryStart(economy, Time.time);
        }

        /// <summary>
        /// Starts a paid play and opens the menu. Returns false when the play cannot
        /// be afforded or the machine is not powered; nothing is charged then.
        /// </summary>
        public bool TryStart(EconomyService economy, float atSeconds)
        {
            if (IsPlaying || economy == null || !IsUnlocked)
            {
                return false;
            }

            if (!economy.TrySpend(_cost, "Arcade play", atSeconds))
            {
                return false;
            }

            PlaysStarted += 1;

            if (_gameKind == ArcadeGameKind.Maze)
            {
                _maze = new MazeGame(new MazeLayout());
                _mazeOverSeconds = 0f;
                MazeGameView view = EnsureMazeView();
                if (view != null)
                {
                    view.Fullscreen = true;
                    view.Show(_maze);
                }

                ArcadeMode.Enter();
                return true;
            }

            _session = new FightSession();
            _session.SetNames("CIVIC", _opponentName);

            // Arcade Paradise flow: one interaction drops you into the game. The
            // cabinet's PLAY/QUIT menu is skipped; Esc leaves the cabinet.
            _session.Confirm();
            if (_view != null)
            {
                _view.SetTheme(_accentColor, _opponentColor);
                _view.Fullscreen = true;
                _view.Show(_session);
            }

            ArcadeMode.Enter();
            return true;
        }

        /// <summary>Closes the session, blanks the screen, and returns to the shop.</summary>
        public void Stop()
        {
            _session = null;
            _maze = null;
            _mazeOverSeconds = 0f;
            if (_view != null)
            {
                _view.Fullscreen = false;
                _view.Hide();
            }

            if (_mazeView != null)
            {
                _mazeView.Fullscreen = false;
                _mazeView.Hide();
            }

            ArcadeMode.Exit();
        }

        private MazeGameView EnsureMazeView()
        {
            if (_mazeView != null)
            {
                return _mazeView;
            }

            _mazeView = GetComponent<MazeGameView>();
            if (_mazeView == null)
            {
                _mazeView = gameObject.AddComponent<MazeGameView>();
            }

            Renderer screen = _view != null ? _view.Screen : null;
            if (screen != null)
            {
                _mazeView.Configure(screen);
            }

            return _mazeView;
        }

        private void Update()
        {
            if (_maze != null)
            {
                UpdateMaze();
                return;
            }

            if (_session == null)
            {
                return;
            }

            _session.Tick(Time.deltaTime);

            if (_session.ExitRequested)
            {
                Stop();
                return;
            }

            // Leave after the result banner has been held, or whenever the player
            // presses Esc/Q. The menu is never shown in this flow, so a menu state
            // means the result hold expired.
            if (_session.Screen == FightScreen.Menu
                || Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.Q))
            {
                Stop();
                return;
            }

            HandleInput(_session);
        }

        private void UpdateMaze()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
            {
                Stop();
                return;
            }

            Vector2Int direction = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                direction = Vector2Int.left;
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                direction = Vector2Int.right;
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                direction = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                direction = Vector2Int.down;
            }

            if (direction != Vector2Int.zero)
            {
                _maze.TryMovePlayer(direction);
            }

            _maze.Tick(Time.deltaTime);

            if (!_maze.IsOver)
            {
                _mazeOverSeconds = 0f;
                return;
            }

            _mazeOverSeconds += Time.deltaTime;
            if (_mazeOverSeconds >= 3f)
            {
                Stop();
            }
        }

        private static void HandleInput(FightSession session)
        {
            if (session.Screen == FightScreen.Menu)
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    session.MoveSelection(-1);
                }
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    session.MoveSelection(1);
                }

                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
                {
                    session.Confirm();
                }

                return;
            }

            if (session.Screen != FightScreen.Fight)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                session.TryFightAction(FightAction.MoveLeft);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                session.TryFightAction(FightAction.MoveRight);
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                session.TryFightAction(FightAction.Punch);
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                session.TryFightAction(FightAction.Kick);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                session.TryFightAction(FightAction.Block);
            }
        }
    }
}
