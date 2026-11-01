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
        [SerializeField] private int _cost = 2;
        [SerializeField] private string _prompt = "Play arcade";

        private FightSession _session;

        public bool IsPlaying => _session != null;

        /// <summary>
        /// False until the machine has been bought and activated. A cabinet with no
        /// service wired stays unlocked so isolated tests and prefab authoring work.
        /// </summary>
        public bool IsUnlocked => _service == null || _service.IsActive;

        public int PlaysStarted { get; private set; }

        public FightSession Session => _session;

        public int Cost => _cost;

        public void Configure(FightGameView view, CustomerSpawner spawner)
        {
            _view = view;
            _spawner = spawner;
        }

        /// <summary>Links the cabinet to the business loop that powers it.</summary>
        public void SetService(ArcadeService service)
        {
            _service = service;
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
            _session = new FightSession();

            // Arcade Paradise flow: one interaction drops you into the game. The
            // cabinet's PLAY/QUIT menu is skipped; Esc leaves the cabinet.
            _session.Confirm();
            if (_view != null)
            {
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
            if (_view != null)
            {
                _view.Fullscreen = false;
                _view.Hide();
            }

            ArcadeMode.Exit();
        }

        private void Update()
        {
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
