using LaundryLegends.Arcade;
using LaundryLegends.Core;
using LaundryLegends.Economy;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Rules tests for the maze-chase game and the cabinet's paid play.
    /// </summary>
    public sealed class ArcadeGameTests
    {
        [Test]
        public void PlayerCannotWalkIntoAWall()
        {
            var layout = new MazeLayout();
            var game = new MazeGame(layout);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in directions)
            {
                if (layout.TryStep(game.Player, direction, out _))
                {
                    continue;
                }

                Assert.IsFalse(game.TryMovePlayer(direction), "A wall must block movement.");
                Assert.AreEqual(layout.SpawnPoint, game.Player);
                return;
            }

            Assert.Ignore("No wall borders the spawn tile in this maze.");
        }

        [Test]
        public void EatingAPelletScoresAndConsumesIt()
        {
            var layout = new MazeLayout();
            var game = new MazeGame(layout);
            int before = game.PelletsRemaining;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in directions)
            {
                if (!layout.TryStep(game.Player, direction, out Vector2Int target))
                {
                    continue;
                }

                MazeTile tile = layout.GetTile(target.x, target.y);
                if (tile != MazeTile.Dot && tile != MazeTile.PowerPellet)
                {
                    continue;
                }

                Assert.IsTrue(game.TryMovePlayer(direction));
                Assert.AreEqual(before - 1, game.PelletsRemaining);
                Assert.AreEqual(10, game.Score);
                return;
            }

            Assert.Ignore("Spawn has no adjacent pellet to step onto.");
        }

        [Test]
        public void RestartRestoresPelletsAndScore()
        {
            var game = new MazeGame(new MazeLayout());
            int total = game.PelletsRemaining;

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int direction in directions)
            {
                if (game.TryMovePlayer(direction))
                {
                    break;
                }
            }

            game.Restart();
            Assert.AreEqual(total, game.PelletsRemaining);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(3, game.Lives);
        }

        [Test]
        public void CabinetChargesOncePerPlay()
        {
            var wallet = new GameState(10);
            var economy = new EconomyService(wallet);
            var go = new GameObject("Cabinet");
            var cabinet = go.AddComponent<ArcadeCabinet>();

            Assert.IsTrue(cabinet.TryStart(economy, 0f));
            Assert.AreEqual(8, wallet.Money);
            Assert.AreEqual(1, cabinet.PlaysStarted);
            Assert.IsTrue(cabinet.IsPlaying);
            Assert.IsFalse(cabinet.TryStart(economy, 1f), "Starting twice must be rejected.");
            Assert.AreEqual(8, wallet.Money);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CabinetRejectsUnaffordablePlay()
        {
            var wallet = new GameState(1);
            var economy = new EconomyService(wallet);
            var go = new GameObject("Cabinet");
            var cabinet = go.AddComponent<ArcadeCabinet>();

            Assert.IsFalse(cabinet.TryStart(economy, 0f));
            Assert.AreEqual(1, wallet.Money, "An unaffordable play must not charge.");
            Assert.AreEqual(0, cabinet.PlaysStarted);
            Assert.IsFalse(cabinet.IsPlaying);

            Object.DestroyImmediate(go);
        }
    }
}
