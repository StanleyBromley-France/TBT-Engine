namespace Setup.Config;

public sealed class MapGenConfig
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Dictionary<string, double> TileDistribution { get; set; } = new();
}
