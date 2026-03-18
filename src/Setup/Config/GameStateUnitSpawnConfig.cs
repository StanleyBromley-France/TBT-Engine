namespace Setup.Config;

public sealed class GameStateUnitSpawnConfig
{
    public int? UnitInstanceId { get; set; }
    public string UnitTemplateId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int Q { get; set; }
    public int R { get; set; }
}
