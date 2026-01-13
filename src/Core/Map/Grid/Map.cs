namespace Core.Map.Grid;

using Algorithms;
using Core.Domain.Types;
using Core.Map.Algorithms;

/// <summary>
/// Represents a hex grid composed of tiles stored in offset coordinates.
/// Map is flat top odd q.
/// </summary>
public sealed class Map
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// Two-dimensional array of tiles in offset (col, row) coordinates.
    /// </summary>
    public Tile[,] Tiles { get; }

    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];

        for (int col = 0; col < width; col++)
        {
            for (int row = 0; row < height; row++)
            {
                Tiles[col, row] = new Tile
                {
                    Terrain = TerrainType.Plain
                };
            }
        }
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
    public bool TryGetTile(HexCoord coord, out Tile tile)
    {
        tile = null!;

        // Convert hex coord to offset indices (col, row)
        var (col, row) = HexCoordConverter.ToOffset(coord);

        // Bounds check against map storage
        if (!IsInside(col, row))
            return false;

        tile = Tiles[col, row];
        return tile != null;
    }

    /// <summary>
    /// Retrieves the tile at a given axial coordinate or null if it doesnt exist.
    /// </summary>
    public Tile? GetTile(HexCoord coord)
    {
        return TryGetTile(coord, out var tile) ? tile : null;
    }
}