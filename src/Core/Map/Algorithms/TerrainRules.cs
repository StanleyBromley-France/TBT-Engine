namespace Core.Map.Algorithms;

using Grid;

/// <summary>
/// Provides rules derived from terrain types.
/// </summary>
public static class TerrainRules
{
    public static bool IsWalkable(TerrainType terrain) =>
        terrain switch
        {
            TerrainType.Plain => true,
            TerrainType.Forest => true,
            TerrainType.Mountain => false,
            TerrainType.Water => false,
            _ => false
        };
}

