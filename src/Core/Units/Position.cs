namespace Core.Units
{
    /// <summary>
    /// Immutable representation of a coordinate on the map grid
    /// Used by Unit and movement logic to describe locations in the battlefield
    ///
    /// Fields:
    /// - X: Horizontal tile index
    /// - Y: Vertical tile index
    ///
    /// Methods:
    /// - ToString: Converts position to string
    /// </summary>
    public struct Position
    {
        public int X { get; }
        public int Y { get; }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Converts position to string
        /// Used for UI/Logs
        /// </summary>
        public readonly override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
