namespace Setup.Build.GameState.Map;

using Core.Game.Bootstrap.Contracts;
using Core.Map.Terrain;

public sealed class MapSpec : IMapSpec
{
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyDictionary<TerrainType, double> TileDistribution { get; }

    public MapSpec(
        int width,
        int height,
        IReadOnlyDictionary<TerrainType, double> tileDistribution)
    {
        Width = width;
        Height = height;
        TileDistribution = tileDistribution ?? throw new ArgumentNullException(nameof(tileDistribution));
    }
}
