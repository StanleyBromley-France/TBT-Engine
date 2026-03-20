namespace Core.Game.Bootstrap.Contracts;

using Core.Map.Terrain;

public interface IMapSpec
{
    int Width { get; }
    int Height { get; }
    IReadOnlyDictionary<TerrainType, double> TileDistribution { get; }
}
