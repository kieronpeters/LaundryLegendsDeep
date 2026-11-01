using LaundryLegends.Arcade;
using NUnit.Framework;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// The cabinet's screen flow: PLAY/QUIT menu, the fight, the five-second result
    /// hold, and the return to the menu or out to the shop.
    /// </summary>
    public sealed class FightSessionTests
    {
        [Test]
        public void SessionStartsOnTheMenuWithPlaySelected()
        {
            var session = new FightSession();

            Assert.AreEqual(FightScreen.Menu, session.Screen);
            Assert.AreEqual(FightSession.PlayOption, session.Selection);
            Assert.IsFalse(session.ExitRequested);
        }

        [Test]
        public void SelectionWrapsBetweenPlayAndQuit()
        {
            var session = new FightSession();

            session.MoveSelection(1);
            Assert.AreEqual(FightSession.QuitOption, session.Selection);

            session.MoveSelection(1);
            Assert.AreEqual(FightSession.PlayOption, session.Selection);

            session.MoveSelection(-1);
            Assert.AreEqual(FightSession.QuitOption, session.Selection);
        }

        [Test]
        public void ChoosingPlayStartsAFightAgainstBison()
        {
            var session = new FightSession();

            Assert.IsTrue(session.Confirm());
            Assert.AreEqual(FightScreen.Fight, session.Screen);
            Assert.IsNotNull(session.Game);
            Assert.AreEqual("HONDA", session.PlayerName);
            Assert.AreEqual("BISON", session.OpponentName);
        }

        [Test]
        public void ChoosingQuitLeavesTheCabinet()
        {
            var session = new FightSession();
            session.MoveSelection(1);

            Assert.IsFalse(session.Confirm());
            Assert.IsTrue(session.ExitRequested);
            Assert.AreEqual(FightScreen.Menu, session.Screen);
        }

        [Test]
        public void ResultHoldsForFiveSecondsThenReturnsToTheMenu()
        {
            var session = new FightSession();
            session.Confirm();

            // Run the fight out by starving the player: no input, the AI wins.
            for (int i = 0; i < 4000 && session.Screen == FightScreen.Fight; i++)
            {
                session.Tick(0.05f);
            }

            Assert.AreEqual(FightScreen.Result, session.Screen);
            Assert.AreEqual(FightSession.ResultHoldSeconds, session.ResultRemaining, 0.001f);

            session.Tick(4.9f);
            Assert.AreEqual(FightScreen.Result, session.Screen, "The result should still be showing.");

            session.Tick(0.2f);
            Assert.AreEqual(FightScreen.Menu, session.Screen, "After five seconds the cabinet returns to the menu.");
            Assert.IsNull(session.Game);
            Assert.AreEqual(FightSession.PlayOption, session.Selection);
        }

        [Test]
        public void PunchOutsideRangeIsRejectedAndInsideRangeDamages()
        {
            var game = new FightGame(1234);
            int startHealth = game.OpponentHealth;

            // Apart: nothing should land.
            Assert.IsFalse(game.TryAction(FightAction.Punch));
            Assert.AreEqual(startHealth, game.OpponentHealth);

            // Close the distance by walking, then punch.
            for (int i = 0; i < 40 && game.Distance > 0.2f; i++)
            {
                game.TryAction(FightAction.MoveRight);
                game.Tick(0.05f);
            }

            Assert.LessOrEqual(game.Distance, 0.2f, "The player should be able to close the distance.");
            Assert.IsTrue(game.TryAction(FightAction.Punch));

            for (int i = 0; i < 6; i++)
            {
                game.Tick(0.05f);
            }

            Assert.Less(game.OpponentHealth, startHealth, "A punch in range should damage the opponent.");
        }

        [Test]
        public void DepletedHealthEndsTheRound()
        {
            var game = new FightGame(7);
            int guard = 0;

            while (!game.IsOver && guard++ < 5000)
            {
                if (game.Distance > 0.2f)
                {
                    game.TryAction(FightAction.MoveRight);
                }
                else
                {
                    game.TryAction(FightAction.Kick);
                }

                game.Tick(0.05f);
            }

            Assert.IsTrue(game.IsOver, "Running the clock or a knockout must end the round.");
            Assert.AreNotEqual(FightOutcome.InProgress, game.Outcome);
        }
    }
}
