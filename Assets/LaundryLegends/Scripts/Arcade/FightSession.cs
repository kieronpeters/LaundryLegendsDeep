using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>Which screen the cabinet is showing.</summary>
    public enum FightScreen
    {
        Menu,
        Fight,
        Result
    }

    /// <summary>
    /// The cabinet's session flow: an attract-style menu with PLAY and QUIT, the
    /// fight itself, and a short result hold before returning to the menu.
    /// <para>
    /// Quitting raises <see cref="ExitRequested"/>; the cabinet then closes the
    /// session and hands the player back to the laundromat.
    /// </para>
    /// </summary>
    public sealed class FightSession
    {
        public const float ResultHoldSeconds = 5f;
        public const int PlayOption = 0;
        public const int QuitOption = 1;

        private float _resultRemaining;

        public FightScreen Screen { get; private set; } = FightScreen.Menu;

        /// <summary>Highlighted menu row: 0 = PLAY, 1 = QUIT.</summary>
        public int Selection { get; private set; } = PlayOption;

        public bool ExitRequested { get; private set; }

        public FightGame Game { get; private set; }

        public float ResultRemaining => Mathf.Max(0f, _resultRemaining);

        /// <summary>The player's fighter; the opponent is the computer.</summary>
        public string PlayerName => "CIVIC";

        public string OpponentName => "BULL";

        /// <summary>Moves the menu highlight, wrapping between the two rows.</summary>
        public void MoveSelection(int delta)
        {
            if (Screen != FightScreen.Menu || ExitRequested)
            {
                return;
            }

            Selection = (Selection + delta + 2) % 2;
        }

        /// <summary>Chooses the highlighted row. Returns true when a fight started.</summary>
        public bool Confirm()
        {
            if (Screen != FightScreen.Menu || ExitRequested)
            {
                return false;
            }

            if (Selection == QuitOption)
            {
                ExitRequested = true;
                return false;
            }

            Game = new FightGame(0);
            Screen = FightScreen.Fight;
            return true;
        }

        /// <summary>Feeds one fight input while fighting.</summary>
        public bool TryFightAction(FightAction action)
        {
            return Screen == FightScreen.Fight && Game != null && Game.TryAction(action);
        }

        /// <summary>Advances the session; the result screen expires after five seconds.</summary>
        public void Tick(float deltaSeconds)
        {
            float step = Mathf.Max(0f, deltaSeconds);

            if (Screen == FightScreen.Fight && Game != null)
            {
                Game.Tick(step);
                if (Game.IsOver)
                {
                    Screen = FightScreen.Result;
                    _resultRemaining = ResultHoldSeconds;
                }

                return;
            }

            if (Screen == FightScreen.Result)
            {
                _resultRemaining -= step;
                if (_resultRemaining <= 0f)
                {
                    BackToMenu();
                }
            }
        }

        /// <summary>Returns to the PLAY/QUIT menu, discarding the finished fight.</summary>
        public void BackToMenu()
        {
            Screen = FightScreen.Menu;
            Selection = PlayOption;
            Game = null;
            _resultRemaining = 0f;
        }
    }
}
