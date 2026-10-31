namespace LaundryLegends.Arcade
{
    /// <summary>
    /// The kind of a single cell in an <see cref="MazeLayout"/>.
    /// </summary>
    public enum MazeTile
    {
        /// <summary>A solid wall. Never walkable.</summary>
        Wall,

        /// <summary>Walkable corridor carrying one dot worth 10 points.</summary>
        Dot,

        /// <summary>Walkable corridor carrying one power pellet worth 50 points.</summary>
        PowerPellet,

        /// <summary>Walkable corridor with no pellet. Also used for the player spawn tile.</summary>
        Empty,

        /// <summary>Ghost-house floor. Not walkable for the player.</summary>
        House,

        /// <summary>The ghost-house doorway. Not walkable for the player.</summary>
        Door,

        /// <summary>
        /// A tunnel mouth. Walkable, and walking outward from it wraps to the opposite edge
        /// of the same row.
        /// </summary>
        Tunnel
    }
}
