using System.Collections.Generic;
using LaundryLegends.Arcade;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the maze layout parsed from the maze-chase design spec.
    /// </summary>
    public sealed class MazeLayoutTests
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        [Test]
        public void AuthoredMazeIsTwentyOneRowsOfTwentyOneCharacters()
        {
            MazeLayout layout = new MazeLayout();

            Assert.AreEqual(21, MazeLayout.AuthoredRows.Count, "The authored maze must have 21 rows.");
            Assert.AreEqual(21, layout.Rows);
            Assert.AreEqual(21, layout.Columns);

            for (int y = 0; y < MazeLayout.AuthoredRows.Count; y++)
            {
                Assert.AreEqual(
                    21,
                    MazeLayout.AuthoredRows[y].Length,
                    $"Row {y} must be exactly 21 characters.");
            }
        }

        [Test]
        public void AuthoredMazeHasOneSpawnOneDoorNineHouseTilesAndTwoTunnelMouths()
        {
            MazeLayout layout = new MazeLayout();

            int spawnCount = 0;
            int doorCount = 0;
            int houseCount = 0;
            int tunnelCount = 0;

            foreach (string row in MazeLayout.AuthoredRows)
            {
                foreach (char symbol in row)
                {
                    switch (symbol)
                    {
                        case 'P':
                            spawnCount++;
                            break;
                        case '=':
                            doorCount++;
                            break;
                        case 'g':
                            houseCount++;
                            break;
                        case 'T':
                            tunnelCount++;
                            break;
                    }
                }
            }

            Assert.AreEqual(1, spawnCount, "The authored maze must have exactly one spawn.");
            Assert.AreEqual(1, doorCount, "The authored maze must have exactly one house door.");
            Assert.AreEqual(9, houseCount, "The authored maze must have nine house tiles.");
            Assert.AreEqual(2, tunnelCount, "The authored maze must have exactly two tunnel mouths.");
            Assert.AreEqual(MazeTile.Door, layout.GetTile(layout.DoorTile.x, layout.DoorTile.y));
            Assert.AreEqual(MazeTile.House, layout.GetTile(layout.HouseCenter.x, layout.HouseCenter.y));
            Assert.AreEqual(MazeTile.Tunnel, layout.GetTile(0, 10));
            Assert.AreEqual(MazeTile.Tunnel, layout.GetTile(20, 10));
        }

        [Test]
        public void LayoutIsLeftRightMirrorSymmetric()
        {
            MazeLayout layout = new MazeLayout();

            for (int y = 0; y < layout.Rows; y++)
            {
                for (int x = 0; x < layout.Columns; x++)
                {
                    int mirrorX = layout.Columns - 1 - x;
                    MazeTile tile = layout.GetTile(x, y);
                    MazeTile mirror = layout.GetTile(mirrorX, y);

                    Assert.AreEqual(
                        tile,
                        mirror,
                        $"Tile ({x}, {y}) is {tile} but its mirror ({mirrorX}, {y}) is {mirror}.");
                    Assert.AreEqual(
                        layout.IsWalkable(x, y),
                        layout.IsWalkable(mirrorX, y),
                        $"Walkability differs between ({x}, {y}) and ({mirrorX}, {y}).");
                    Assert.AreEqual(
                        PelletClass(tile),
                        PelletClass(mirror),
                        $"Pellet type differs between ({x}, {y}) and ({mirrorX}, {y}).");
                }
            }
        }

        [Test]
        public void FloodFillFromSpawnReachesEveryWalkableTileWithoutWrap()
        {
            MazeLayout layout = new MazeLayout();

            HashSet<Vector2Int> reached = FloodFill(layout, false);

            Assert.AreEqual(
                layout.WalkableCount,
                reached.Count,
                "Without tunnel wrap the flood fill must reach every walkable tile.");
            AssertEveryEdibleTileReached(layout, reached);
        }

        [Test]
        public void FloodFillFromSpawnReachesEveryWalkableTileWithTunnelWrap()
        {
            MazeLayout layout = new MazeLayout();

            HashSet<Vector2Int> reached = FloodFill(layout, true);

            Assert.AreEqual(
                layout.WalkableCount,
                reached.Count,
                "With tunnel wrap the flood fill must reach every walkable tile.");
            AssertEveryEdibleTileReached(layout, reached);
        }

        [Test]
        public void EveryWalkableTileHasAtLeastTwoWalkableNeighbours()
        {
            MazeLayout layout = new MazeLayout();

            for (int y = 0; y < layout.Rows; y++)
            {
                for (int x = 0; x < layout.Columns; x++)
                {
                    if (!layout.IsWalkable(x, y))
                    {
                        continue;
                    }

                    if (layout.GetTile(x, y) == MazeTile.Tunnel)
                    {
                        // A tunnel mouth is exempt: it wraps instead of joining two interior tiles.
                        continue;
                    }

                    int neighbours = 0;

                    foreach (Vector2Int direction in CardinalDirections)
                    {
                        int nextX = x + direction.x;
                        int nextY = y + direction.y;

                        if (nextY < 0 || nextY >= layout.Rows)
                        {
                            continue;
                        }

                        nextX = (nextX + layout.Columns) % layout.Columns;

                        if (layout.IsWalkable(nextX, nextY))
                        {
                            neighbours++;
                        }
                    }

                    Assert.GreaterOrEqual(
                        neighbours,
                        2,
                        $"Walkable tile ({x}, {y}) is a dead end with {neighbours} walkable neighbour(s).");
                }
            }
        }

        [Test]
        public void CountsMatchTheSpec()
        {
            MazeLayout layout = new MazeLayout();

            Assert.AreEqual(184, layout.DotCount, "The maze must contain 184 dots.");
            Assert.AreEqual(4, layout.PowerPelletCount, "The maze must contain 4 power pellets.");
            Assert.AreEqual(194, layout.WalkableCount, "The maze must contain 194 walkable tiles.");
            Assert.AreEqual(
                188,
                layout.DotCount + layout.PowerPelletCount,
                "The maze must contain 188 edible tiles.");
        }

        [Test]
        public void SpecialTilesMatchTheSpec()
        {
            MazeLayout layout = new MazeLayout();

            Assert.AreEqual(new Vector2Int(10, 16), layout.SpawnPoint);
            Assert.AreEqual(new Vector2Int(10, 10), layout.HouseCenter);
            Assert.AreEqual(new Vector2Int(10, 8), layout.DoorTile);
            Assert.AreEqual(MazeTile.Empty, layout.GetTile(10, 16));

            Vector2Int[] powerPellets =
            {
                new Vector2Int(1, 1),
                new Vector2Int(19, 1),
                new Vector2Int(1, 19),
                new Vector2Int(19, 19)
            };

            foreach (Vector2Int pellet in powerPellets)
            {
                Assert.AreEqual(
                    MazeTile.PowerPellet,
                    layout.GetTile(pellet.x, pellet.y),
                    $"Expected a power pellet at ({pellet.x}, {pellet.y}).");
            }

            for (int y = 9; y <= 11; y++)
            {
                for (int x = 9; x <= 11; x++)
                {
                    Assert.IsFalse(
                        layout.IsWalkable(x, y),
                        $"House tile ({x}, {y}) must not be walkable for the player.");
                }
            }

            Assert.IsFalse(
                layout.IsWalkable(layout.DoorTile.x, layout.DoorTile.y),
                "The house door must not be walkable for the player.");
        }

        [Test]
        public void TryStepIntoWallIsRejected()
        {
            MazeLayout layout = new MazeLayout();
            Vector2Int spawn = layout.SpawnPoint;

            Assert.AreEqual(MazeTile.Wall, layout.GetTile(spawn.x, spawn.y - 1));

            bool stepped = layout.TryStep(spawn, new Vector2Int(0, -1), out Vector2Int next);

            Assert.IsFalse(stepped, "Stepping into a wall must be rejected.");
            Assert.AreEqual(spawn, next, "A rejected step must leave the position unchanged.");
        }

        [Test]
        public void TryStepOntoOpenFloorSucceeds()
        {
            MazeLayout layout = new MazeLayout();
            Vector2Int spawn = layout.SpawnPoint;

            bool stepped = layout.TryStep(spawn, new Vector2Int(-1, 0), out Vector2Int next);

            Assert.IsTrue(stepped, "Stepping onto adjacent open floor must succeed.");
            Assert.AreEqual(new Vector2Int(9, 16), next);
            Assert.IsTrue(layout.IsWalkable(next.x, next.y));
        }

        [Test]
        public void TryStepThroughTunnelWrapsToOppositeEdge()
        {
            MazeLayout layout = new MazeLayout();

            Assert.IsTrue(
                layout.TryStep(new Vector2Int(0, 10), new Vector2Int(-1, 0), out Vector2Int leftExit),
                "Walking off the left edge of the tunnel row must wrap.");
            Assert.AreEqual(new Vector2Int(20, 10), leftExit);

            Assert.IsTrue(
                layout.TryStep(new Vector2Int(20, 10), new Vector2Int(1, 0), out Vector2Int rightExit),
                "Walking off the right edge of the tunnel row must wrap.");
            Assert.AreEqual(new Vector2Int(0, 10), rightExit);

            Assert.IsTrue(
                layout.TryStep(new Vector2Int(1, 10), new Vector2Int(-1, 0), out Vector2Int tunnelMouth),
                "Stepping onto the tunnel mouth must not wrap yet.");
            Assert.AreEqual(new Vector2Int(0, 10), tunnelMouth);
        }

        [Test]
        public void TryStepIntoHouseDoorIsRejectedForThePlayer()
        {
            MazeLayout layout = new MazeLayout();

            Assert.AreEqual(new Vector2Int(10, 8), layout.DoorTile);
            Assert.AreEqual(MazeTile.Empty, layout.GetTile(10, 7));

            bool stepped = layout.TryStep(new Vector2Int(10, 7), new Vector2Int(0, 1), out Vector2Int next);

            Assert.IsFalse(stepped, "The player must not be able to step onto the house door.");
            Assert.AreEqual(new Vector2Int(10, 7), next);
        }

        [Test]
        public void TryStepRejectsNonCardinalDirections()
        {
            MazeLayout layout = new MazeLayout();
            Vector2Int spawn = layout.SpawnPoint;

            Assert.IsFalse(layout.TryStep(spawn, new Vector2Int(1, 1), out _));
            Assert.IsFalse(layout.TryStep(spawn, new Vector2Int(0, 0), out _));
            Assert.IsFalse(layout.TryStep(spawn, new Vector2Int(2, 0), out _));
        }

        private static void AssertEveryEdibleTileReached(
            MazeLayout layout,
            HashSet<Vector2Int> reached)
        {
            int edibleCount = 0;

            for (int y = 0; y < layout.Rows; y++)
            {
                for (int x = 0; x < layout.Columns; x++)
                {
                    MazeTile tile = layout.GetTile(x, y);

                    if (!IsPellet(tile))
                    {
                        continue;
                    }

                    edibleCount++;
                    Assert.IsTrue(
                        reached.Contains(new Vector2Int(x, y)),
                        $"Edible tile ({x}, {y}) is unreachable from the spawn.");
                }
            }

            Assert.AreEqual(188, edibleCount);
        }

        private static bool IsPellet(MazeTile tile)
        {
            return tile == MazeTile.Dot || tile == MazeTile.PowerPellet;
        }

        private static int PelletClass(MazeTile tile)
        {
            switch (tile)
            {
                case MazeTile.Dot:
                    return 1;
                case MazeTile.PowerPellet:
                    return 2;
                default:
                    return 0;
            }
        }

        private static HashSet<Vector2Int> FloodFill(MazeLayout layout, bool wrapTunnel)
        {
            var reached = new HashSet<Vector2Int> { layout.SpawnPoint };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(layout.SpawnPoint);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (Vector2Int direction in CardinalDirections)
                {
                    int nextX = current.x + direction.x;
                    int nextY = current.y + direction.y;

                    if (nextY < 0 || nextY >= layout.Rows)
                    {
                        continue;
                    }

                    if (nextX < 0 || nextX >= layout.Columns)
                    {
                        if (!wrapTunnel)
                        {
                            continue;
                        }

                        nextX = (nextX + layout.Columns) % layout.Columns;
                    }

                    if (!layout.IsWalkable(nextX, nextY))
                    {
                        continue;
                    }

                    var next = new Vector2Int(nextX, nextY);

                    if (reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return reached;
        }
    }
}
