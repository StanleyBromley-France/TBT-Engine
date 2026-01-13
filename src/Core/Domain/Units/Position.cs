namespace Core.Domain.Units
{
    /// <summary>
    /// Immutable representation of a coordinate on the map grid
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
        /// </summary>
        public readonly override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
