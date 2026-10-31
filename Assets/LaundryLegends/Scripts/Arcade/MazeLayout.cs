using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// A parsed, immutable maze grid for the cabinet maze-chase game.
    /// </summary>
    /// <remarks>
    /// Coordinates are grid coordinates: <c>x</c> increases to the right and <c>y</c> is the row
    /// index counted from the top of the ASCII layout, so (0, 0) is the top-left tile. This matches
    /// the authored layout; a renderer is responsible for flipping Y if it works in world space
    /// where +Y points up. This type is pure C# apart from <see cref="Vector2Int"/>.
    /// </remarks>
    public sealed class MazeLayout
    {
        /// <summary>Number of rows in the authored maze.</summary>
        public const int AuthoredRowCount = 21;

        /// <summary>Number of columns in the authored maze.</summary>
        public const int AuthoredColumnCount = 21;

        private const char WallChar = '#';
        private const char DotChar = '.';
        private const char PowerPelletChar = 'o';
        private const char EmptyChar = ' ';
        private const char HouseChar = 'g';
        private const char DoorChar = '=';
        private const char TunnelChar = 'T';
        private const char SpawnChar = 'P';

        private static readonly string[] AuthoredAscii =
        {
            "#####################",
            "#o.................o#",
            "#.##.####.#.####.##.#",
            "#.##.####.#.####.##.#",
            "#...................#",
            "#.##.#####.#####.##.#",
            "#.##.#####.#####.##.#",
            "#........   ........#",
            "#.#####.##=##.#####.#",
            "#.#####.#ggg#.#####.#",
            "T.......#ggg#.......T",
            "#.#####.#ggg#.#####.#",
            "#.#####.#####.#####.#",
            "#...................#",
            "#.##.####.#.####.##.#",
            "#.##.####.#.####.##.#",
            "#.........P.........#",
            "#.##.##.##.##.##.##.#",
            "#.##.##.##.##.##.##.#",
            "#o.................o#",
            "#####################"
        };

        private static readonly IReadOnlyList<string> ReadOnlyAuthoredAscii =
            Array.AsReadOnly(AuthoredAscii);

        private readonly MazeTile[,] _tiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="MazeLayout"/> class from the maze layout
        /// authored in the design spec.
        /// </summary>
        public MazeLayout()
            : this(AuthoredAscii)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MazeLayout"/> class by parsing ASCII rows.
        /// </summary>
        /// <param name="rows">
        /// Maze rows using <c>#</c> wall, <c>.</c> dot, <c>o</c> power pellet, <c> </c> empty,
        /// <c>g</c> house floor, <c>=</c> house door, <c>T</c> tunnel mouth and <c>P</c> spawn.
        /// Row 0 is the top row. Every row must have the same length.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the rows are empty, have inconsistent lengths, contain an unknown character,
        /// or do not contain exactly one spawn and one door.
        /// </exception>
        public MazeLayout(IReadOnlyList<string> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (rows.Count == 0)
            {
                throw new ArgumentException("At least one row is required.", nameof(rows));
            }

            Rows = rows.Count;
            Columns = rows[0] == null ? 0 : rows[0].Length;

            if (Columns == 0)
            {
                throw new ArgumentException("Maze rows must not be empty.", nameof(rows));
            }

            _tiles = new MazeTile[Columns, Rows];

            int spawnCount = 0;
            int doorCount = 0;
            int dotCount = 0;
            int powerPelletCount = 0;
            int walkableCount = 0;
            int houseMinX = int.MaxValue;
            int houseMinY = int.MaxValue;
            int houseMaxX = int.MinValue;
            int houseMaxY = int.MinValue;

            Vector2Int spawnPoint = default;
            Vector2Int doorTile = default;

            for (int y = 0; y < Rows; y++)
            {
                string row = rows[y];

                if (row == null)
                {
                    throw new ArgumentException($"Row {y} is null.", nameof(rows));
                }

                if (row.Length != Columns)
                {
                    throw new ArgumentException(
                        $"Row {y} has {row.Length} characters; expected {Columns}.",
                        nameof(rows));
                }

                for (int x = 0; x < Columns; x++)
                {
                    char symbol = row[x];
                    MazeTile tile = ToTile(symbol, x, y);

                    if (tile == MazeTile.Dot)
                    {
                        dotCount++;
                    }
                    else if (tile == MazeTile.PowerPellet)
                    {
                        powerPelletCount++;
                    }

                    if (IsWalkableTile(tile))
                    {
                        walkableCount++;
                    }

                    if (tile == MazeTile.House)
                    {
                        houseMinX = Math.Min(houseMinX, x);
                        houseMinY = Math.Min(houseMinY, y);
                        houseMaxX = Math.Max(houseMaxX, x);
                        houseMaxY = Math.Max(houseMaxY, y);
                    }

                    if (symbol == SpawnChar)
                    {
                        spawnCount++;
                        spawnPoint = new Vector2Int(x, y);
                    }

                    if (tile == MazeTile.Door)
                    {
                        doorCount++;
                        doorTile = new Vector2Int(x, y);
                    }

                    _tiles[x, y] = tile;
                }
            }

            if (spawnCount != 1)
            {
                throw new ArgumentException(
                    $"Expected exactly one spawn, found {spawnCount}.",
                    nameof(rows));
            }

            if (doorCount != 1)
            {
                throw new ArgumentException(
                    $"Expected exactly one house door, found {doorCount}.",
                    nameof(rows));
            }

            if (houseMaxX < houseMinX)
            {
                throw new ArgumentException("The maze has no house floor.", nameof(rows));
            }

            DotCount = dotCount;
            PowerPelletCount = powerPelletCount;
            WalkableCount = walkableCount;
            SpawnPoint = spawnPoint;
            DoorTile = doorTile;
            HouseCenter = new Vector2Int(
                (houseMinX + houseMaxX) / 2,
                (houseMinY + houseMaxY) / 2);
        }

        /// <summary>Gets the number of rows in this layout.</summary>
        public int Rows { get; }

        /// <summary>Gets the number of columns in this layout.</summary>
        public int Columns { get; }

        /// <summary>Gets the number of dot tiles.</summary>
        public int DotCount { get; }

        /// <summary>Gets the number of power-pellet tiles.</summary>
        public int PowerPelletCount { get; }

        /// <summary>
        /// Gets the number of tiles a player may occupy. Walls, the house floor and the house door
        /// are excluded.
        /// </summary>
        public int WalkableCount { get; }

        /// <summary>Gets the player spawn tile.</summary>
        public Vector2Int SpawnPoint { get; }

        /// <summary>Gets the centre tile of the house floor, computed from the house bounds.</summary>
        public Vector2Int HouseCenter { get; }

        /// <summary>Gets the house door tile. It is not walkable for the player.</summary>
        public Vector2Int DoorTile { get; }

        /// <summary>
        /// Gets the authored ASCII rows exactly as written in the design spec, for validation.
        /// </summary>
        public static IReadOnlyList<string> AuthoredRows => ReadOnlyAuthoredAscii;

        /// <summary>
        /// Gets the tile at a grid coordinate.
        /// </summary>
        /// <param name="x">Column index, 0 to <see cref="Columns"/> - 1.</param>
        /// <param name="y">Row index from the top, 0 to <see cref="Rows"/> - 1.</param>
        /// <returns>
        /// The tile at that coordinate, or <see cref="MazeTile.Wall"/> when the coordinate is
        /// outside the grid.
        /// </returns>
        public MazeTile GetTile(int x, int y)
        {
            return IsInside(x, y) ? _tiles[x, y] : MazeTile.Wall;
        }

        /// <summary>
        /// Reports whether the player may stand on a tile.
        /// </summary>
        /// <param name="x">Column index.</param>
        /// <param name="y">Row index from the top.</param>
        /// <returns>
        /// <c>true</c> for dot, power-pellet, empty and tunnel tiles; <c>false</c> for walls, the
        /// house floor, the house door and any coordinate outside the grid.
        /// </returns>
        public bool IsWalkable(int x, int y)
        {
            return IsInside(x, y) && IsWalkableTile(_tiles[x, y]);
        }

        /// <summary>
        /// Computes the neighbouring tile reached by taking one step in a cardinal direction.
        /// Horizontal steps outward from a tunnel tile wrap to the opposite edge of the grid;
        /// steps into walls, off the top or bottom edge, and non-cardinal directions are rejected.
        /// </summary>
        /// <param name="from">The tile being left.</param>
        /// <param name="direction">A cardinal step: (1,0), (-1,0), (0,1) or (0,-1).</param>
        /// <param name="to">
        /// The walkable tile stepped onto, or <paramref name="from"/> when the step is rejected.
        /// </param>
        /// <returns><c>true</c> when the step is legal.</returns>
        public bool TryStep(Vector2Int from, Vector2Int direction, out Vector2Int to)
        {
            to = from;

            if (!IsInside(from.x, from.y))
            {
                return false;
            }

            if (Math.Abs(direction.x) + Math.Abs(direction.y) != 1)
            {
                return false;
            }

            int nextX = from.x + direction.x;
            int nextY = from.y + direction.y;

            if (nextY < 0 || nextY >= Rows)
            {
                return false;
            }

            if (nextX < 0 || nextX >= Columns)
            {
                if (_tiles[from.x, from.y] != MazeTile.Tunnel)
                {
                    return false;
                }

                nextX = (nextX + Columns) % Columns;
            }

            if (!IsWalkable(nextX, nextY))
            {
                return false;
            }

            to = new Vector2Int(nextX, nextY);
            return true;
        }

        private static bool IsWalkableTile(MazeTile tile)
        {
            return tile == MazeTile.Dot
                || tile == MazeTile.PowerPellet
                || tile == MazeTile.Empty
                || tile == MazeTile.Tunnel;
        }

        private static MazeTile ToTile(char symbol, int x, int y)
        {
            switch (symbol)
            {
                case WallChar:
                    return MazeTile.Wall;
                case DotChar:
                    return MazeTile.Dot;
                case PowerPelletChar:
                    return MazeTile.PowerPellet;
                case EmptyChar:
                    return MazeTile.Empty;
                case HouseChar:
                    return MazeTile.House;
                case DoorChar:
                    return MazeTile.Door;
                case TunnelChar:
                    return MazeTile.Tunnel;
                case SpawnChar:
                    return MazeTile.Empty;
                default:
                    throw new ArgumentException(
                        $"Unknown maze character '{symbol}' at ({x}, {y}).");
            }
        }

        private bool IsInside(int x, int y)
        {
            return x >= 0 && x < Columns && y >= 0 && y < Rows;
        }
    }
}
