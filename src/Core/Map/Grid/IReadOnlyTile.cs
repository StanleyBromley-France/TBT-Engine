namespace Core.Map.Grid;

using Core.Map.Terrain;

public interface IReadOnlyTile
{
    TerrainType Terrain { get; }
    bool IsWalkable { get; }
}