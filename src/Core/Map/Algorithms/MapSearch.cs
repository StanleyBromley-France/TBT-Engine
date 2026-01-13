namespace Core.Map.Algorithms;

using Core.Domain.Types;
using Map.Grid;

/// <summary>
/// Provides map-bounded hex-coordinate search utilities built on top of
/// <see cref="HexMath"/>. Filters pure hex-math results to only coordinates
/// that exist within a specific map.
/// </summary>

public static class MapSearch
{
    /// <summary>
    /// Returns all neighboring coordinates around a center tile that exist within the map.
    /// </summary>
    public static IEnumerable<HexCoord> GetNeighbourCoords(Map map, HexCoord center)
    {
        foreach (var neighbour in HexMath.GetNeighborCoords(center))
            if (map.TryGetTile(neighbour, out _))
                yield return neighbour;
    }

    /// <summary>
    /// Returns all coordinates within a radius of a center tile that exist within the map.
    /// </summary>
    public static IEnumerable<HexCoord> GetCoordsInRadius(Map map, HexCoord center, int radius)
    {
        foreach (var neighbour in HexMath.GetCoordsInRadius(center, radius))
            if (map.TryGetTile(neighbour, out _))
                yield return neighbour;
    }

    /// <summary>
    /// Returns a ray of coordinates starting from a tile in a given direction,
    /// stopping when the ray leaves the map or the maximum distance is reached.
    /// </summary>
    public static IEnumerable<HexCoord> GetCoordsInRay(Map map, HexCoord start, HexDirection direction, int distance)
    {
        foreach (var neighbour in HexMath.GetCoordsInRay(start, direction, distance))
            if (map.TryGetTile(neighbour, out _))
                yield return neighbour;
            else
                yield break;
    }

    /// <summary>
    /// Returns all neighboring tiles around a center tile that exist within the map.
    /// </summary>
    public static IEnumerable<Tile> GetNeighbourTiles(Map map, HexCoord center)
    {
        foreach (var neighbour in HexMath.GetNeighborCoords(center))
            if (map.TryGetTile(neighbour, out var tile))
                yield return tile;
    }

    /// <summary>
    /// Returns all tiles within a radius of a center tile that exist within the map.
    /// </summary>
    public static IEnumerable<Tile> GetTilesInRadius(Map map, HexCoord center, int radius)
    {
        foreach (var neighbour in HexMath.GetCoordsInRadius(center, radius))
            if (map.TryGetTile(neighbour, out var tile))
                yield return tile;
    }

    /// <summary>
    /// Returns a ray of tiles starting from a tile in a given direction,
    /// stopping when the ray leaves the map or the maximum distance is reached.
    /// </summary>
    public static IEnumerable<Tile> GetTilesInRay(Map map, HexCoord start, HexDirection direction, int distance)
    {
        foreach (var neighbour in HexMath.GetCoordsInRay(start, direction, distance))
            if (map.TryGetTile(neighbour, out var tile))
                yield return tile;
            else
                yield break;
    }
}

