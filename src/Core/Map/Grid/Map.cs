namespace Core.Map.Grid;

using Algorithms;

/// <summary>
/// Represents a hex grid composed of tiles stored in offset coordinates.
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
    /// Retrieves the tile at a given axial coordinate or null if outside.
    /// </summary>
    public Tile? GetTile(HexCoord coord)
    {
        var (col, row) = HexCoordConverter.ToOffset(coord);
        return IsInside(col, row) ? Tiles[col, row] : null;
    }
}