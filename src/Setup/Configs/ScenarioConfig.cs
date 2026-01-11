namespace Setup.Configs;

public sealed class ScenarioConfig
{
    public string Id { get; set; } = "";
    public MapGenConfig MapGen { get; set; } = new();
    public int Seed { get; set; }
    public string FirstTeamToAct { get; set; } = "";
    public List<ScenarioUnitSpawnConfig> Units { get; set; } = [];
}

public sealed class MapGenConfig 
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Dictionary<string, float> TileDistribution { get; set; } = [];
}

public sealed class ScenarioUnitSpawnConfig()
{
    public string UnitTemplateId { get; set; } = "";
    public string Team { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
}
