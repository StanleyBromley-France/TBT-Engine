namespace Setup.Build.GameState.Map;

using Core.Game.Bootstrap.Contracts;
using Core.Map.Terrain;

public sealed class MapSpec : IMapSpec
{
    public int Width { get; }
    public int Height { get; }
    public int Seed { get; }
    public int RngPosition { get; }
    public IReadOnlyDictionary<TerrainType, double> TileDistribution { get; }

    public MapSpec(
        int width,
        int height,
        int seed,
        int rngPosition,
        IReadOnlyDictionary<TerrainType, double> tileDistribution)
    {
        Width = width;
        Height = height;
        Seed = seed;
        RngPosition = rngPosition;
        TileDistribution = tileDistribution ?? throw new ArgumentNullException(nameof(tileDistribution));
    }
}
