using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Grid maze-chase rules: pellets, scoring, lives, and four pursuers that
    /// step toward the player without reversing. Pure logic so it can be tested
    /// without a scene; the view only reads its state.
    /// </summary>
    public sealed class MazeGame
    {
        private const int DotScore = 10;
        private const float GhostStepSeconds = 0.4f;

        private readonly HashSet<Vector2Int> _pellets = new HashSet<Vector2Int>();
        private readonly Vector2Int[] _ghosts = new Vector2Int[4];
        private readonly Vector2Int[] _ghostStart = new Vector2Int[4];
        private readonly Vector2Int[] _ghostPrevious = new Vector2Int[4];
        private readonly System.Random _random;
        private float _ghostTimer;

        public MazeGame(MazeLayout layout, int seed = 7)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _random = new System.Random(seed);

            for (int y = 0; y < layout.Rows; y++)
            {
                for (int x = 0; x < layout.Columns; x++)
                {
                    MazeTile tile = layout.GetTile(x, y);
                    if (tile == MazeTile.Dot || tile == MazeTile.PowerPellet)
                    {
                        _pellets.Add(new Vector2Int(x, y));
                    }
                }
            }

            Player = layout.SpawnPoint;
            ResetGhosts();
            Lives = 3;
        }

        public MazeLayout Layout { get; }

        public Vector2Int Player { get; private set; }

        public IReadOnlyList<Vector2Int> Ghosts => _ghosts;

        public int Score { get; private set; }

        public int Lives { get; private set; }

        public int PelletsRemaining => _pellets.Count;

        public bool IsWon { get; private set; }

        public bool IsLost { get; private set; }

        public bool IsOver => IsWon || IsLost;

        /// <summary>Moves the player one tile if the direction is walkable; eats what is there.</summary>
        public bool TryMovePlayer(Vector2Int direction)
        {
            if (IsOver || direction == Vector2Int.zero)
            {
                return false;
            }

            if (!Layout.TryStep(Player, direction, out Vector2Int next))
            {
                return false;
            }

            Player = next;

            if (_pellets.Remove(next))
            {
                Score += DotScore;
                if (_pellets.Count == 0)
                {
                    IsWon = true;
                }
            }

            CheckCollisions();
            return true;
        }

        /// <summary>Advances pursuer movement; called every frame with the frame delta.</summary>
        public void Tick(float deltaSeconds)
        {
            if (IsOver)
            {
                return;
            }

            _ghostTimer += deltaSeconds;
            while (_ghostTimer >= GhostStepSeconds)
            {
                _ghostTimer -= GhostStepSeconds;
                StepGhosts();
                CheckCollisions();
            }
        }

        public void Restart()
        {
            Score = 0;
            Lives = 3;
            IsWon = false;
            IsLost = false;
            _ghostTimer = 0f;
            Player = Layout.SpawnPoint;
            ResetGhosts();

            _pellets.Clear();
            for (int y = 0; y < Layout.Rows; y++)
            {
                for (int x = 0; x < Layout.Columns; x++)
                {
                    MazeTile tile = Layout.GetTile(x, y);
                    if (tile == MazeTile.Dot || tile == MazeTile.PowerPellet)
                    {
                        _pellets.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        private void ResetGhosts()
        {
            Vector2Int house = Layout.HouseCenter;
            _ghostStart[0] = house;
            _ghostStart[1] = new Vector2Int(house.x - 1, house.y);
            _ghostStart[2] = new Vector2Int(house.x + 1, house.y);
            _ghostStart[3] = new Vector2Int(house.x, house.y + 1);

            for (int i = 0; i < _ghosts.Length; i++)
            {
                _ghosts[i] = _ghostStart[i];
                _ghostPrevious[i] = _ghostStart[i];
            }
        }

        private void StepGhosts()
        {
            Vector2Int[] directions =
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            for (int i = 0; i < _ghosts.Length; i++)
            {
                Vector2Int best = _ghosts[i];
                int bestDistance = int.MaxValue;
                Vector2Int reverse = _ghostPrevious[i];

                foreach (Vector2Int direction in directions)
                {
                    if (!Layout.TryStep(_ghosts[i], direction, out Vector2Int candidate))
                    {
                        continue;
                    }

                    if (candidate == reverse)
                    {
                        continue;
                    }

                    int distance = Mathf.Abs(candidate.x - Player.x) + Mathf.Abs(candidate.y - Player.y);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate;
                    }
                }

                _ghostPrevious[i] = _ghosts[i];
                _ghosts[i] = best;
            }
        }

        private void CheckCollisions()
        {
            foreach (Vector2Int ghost in _ghosts)
            {
                if (ghost != Player)
                {
                    continue;
                }

                Lives -= 1;
                if (Lives <= 0)
                {
                    Lives = 0;
                    IsLost = true;
                    return;
                }

                Player = Layout.SpawnPoint;
                ResetGhosts();
                return;
            }
        }
    }
}
