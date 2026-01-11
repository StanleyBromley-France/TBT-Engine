namespace Core.Map.Algorithms;

using Core.Map.Grid;
using Core.Types;

/// <summary>
/// Provides common axial-coordinate hex math utilities.
/// </summary>
public static class HexMath
{
    // Axial direction vectors for pointy-top grids (q, r).
    // Order matches the HexDirection enum.
    private static readonly HexCoord[] DirectionVectors =
    {
        new(+1, 0),   // East
        new(+1, -1),  // NorthEast
        new(0, -1),   // NorthWest
        new(-1, 0),   // West
        new(-1, +1),  // SouthWest
        new(0, +1)    // SouthEast
    };

    /// <summary>
    /// Returns a coordinate stepped from the starting position in a given direction.
    /// </summary>
    public static HexCoord GetCoordInDirection(HexCoord from, HexDirection dir, int distance = 1)
    {
        var delta = DirectionVectors[(int)dir];
        return new HexCoord(from.Q + delta.Q * distance, from.R + delta.R * distance);
    }

    /// <summary>
    /// Produces a sequence of coordinates extending from a starting position
    /// in a given direction for a specified distance.
    /// </summary>

    public static IEnumerable<HexCoord> GetCoordsInRay(HexCoord from, HexDirection dir, int distance)
    {
        if (distance <= 0)
            yield break;

        var delta = DirectionVectors[(int)dir];

        for (int i = 1; i <= distance; i++)
        {
            yield return new HexCoord(from.Q + delta.Q * i, from.R + delta.R * i);
        }
    }
    /// <summary>
    /// Gets all six neighboring coordinates around a center tile.
    /// </summary>
    public static IEnumerable<HexCoord> GetNeighborCoords(HexCoord center)
    {
        for (int i = 0; i < DirectionVectors.Length; i++)
            yield return center + DirectionVectors[i];
    }

    /// <summary>
    /// Calculates the hex distance between two axial coordinates. Uses cube distance.
    /// </summary>
    public static int GetDistance(HexCoord a, HexCoord b)
    {
        int dq = a.Q - b.Q;
        int dr = a.R - b.R;
        int dy = -dq - dr;

        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dy)) / 2;
    }

    /// <summary>
    /// Produces all axial coordinates within a radius around a center tile.
    /// </summary>
    public static IEnumerable<HexCoord> GetCoordsInRadius(HexCoord center, int radius)
    {
        for (int dq = -radius; dq <= radius; dq++)
        {
            int minR = Math.Max(-radius, -dq - radius);
            int maxR = Math.Min(radius, -dq + radius);

            for (int dr = minR; dr <= maxR; dr++)
                yield return new HexCoord(center.Q + dq, center.R + dr);
        }
    }
}
