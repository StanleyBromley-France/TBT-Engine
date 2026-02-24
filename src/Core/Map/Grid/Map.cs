namespace Core.Map.Grid;

using Core.Domain.Types;
using Core.Map.Hex;
using Core.Map.Terrain;

/// <summary> 
/// Represents a hex grid composed of tiles stored in offset coordinates.
/// Map is flat top odd q.
/// </summary>
public sealed class Map : IReadOnlyMap
{
    public int Width { get; }
    public int Height { get; }

    private readonly Tile[,] _tiles;

    public Map(Tile[,] tiles)
    {
        if (tiles == null) throw new ArgumentNullException(nameof(tiles));

        Width = tiles.GetLength(0);
        Height = tiles.GetLength(1);

        _tiles = tiles;
    }


    /// <summary> 
    /// Returns true if the given offset coordinate lies inside the map.
    /// </summary>
    public bool IsInside(int col, int row) =>
        col >= 0 && col < Width &&
        row >= 0 && row < Height;

    /// <summary>
    /// Returns true if tile exists at given coord.
    /// Retrieves the tile at a given axial coordinate or null if outside or doesnt exist and stores it in tile.
    /// </summary>
    public bool TryGetTile(HexCoord coord, out IReadOnlyTile tile)
    {
        var (col, row) = HexCoordConverter.ToOffset(coord);

        if (!IsInside(col, row))
        {
            tile = default!;
            return false;
        }

        tile = _tiles[col, row];
        return true;
    }

    /// <summary>
    /// Retrieves the tile at a given axial coordinate or null if it doesnt exist.
    /// </summary>
    public IReadOnlyTile? GetTile(HexCoord coord) =>
        TryGetTile(coord, out var tile) ? tile : null;
}