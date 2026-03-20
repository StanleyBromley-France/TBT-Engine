namespace Setup.Config;

public sealed class GameStateConfig
{
    public string Id { get; set; } = string.Empty;
    public MapGenConfig MapGen { get; set; } = new();
    public int AttackerTeamId { get; set; } = 1;
    public int DefenderTeamId { get; set; } = 2;
    public int TeamToAct { get; set; } = 1;
    public int AttackerTurnsTaken { get; set; }
    public List<GameStateUnitSpawnConfig> Units { get; set; } = new();
}
