namespace Core.Map.Terrain;

/// <summary>
/// Provides rules derived from terrain types.
/// </summary>
public static class TerrainRules
{
    public static bool IsWalkable(TerrainType terrain) =>
        terrain switch
        {
            TerrainType.Plain => true,
            TerrainType.Mountain => false,
            TerrainType.Water => false,
            _ => false
        };
}

