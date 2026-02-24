namespace Core.Map.Grid;

using Core.Map.Terrain;

/// <summary>
/// Represents a single hex tile on the map.
/// </summary>
public sealed class Tile : IReadOnlyTile
{
    /// <summary>
    /// Type of terrain this tile contains.
    /// </summary>
    public TerrainType Terrain { get; set; }

    /// <summary>
    /// Indicates whether units can enter this tile based on its terrain.
    /// </summary>
    public bool IsWalkable => TerrainRules.IsWalkable(Terrain);
}