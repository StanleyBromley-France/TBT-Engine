namespace Setup.Build.GameState;

using Core.Domain.Types;
using Core.Game.Bootstrap.Contracts;

public sealed class GameStateSpec : IGameStateSpec
{
    public string Id { get; }
    public IMapSpec MapSpec { get; }
    public TeamId AttackerTeamId { get; }
    public TeamId DefenderTeamId { get; }
    public Turn InitialTurn { get; }
    public IReadOnlyList<IUnitSpawnSpec> UnitSpawns { get; }

    public GameStateSpec(
        string id,
        IMapSpec mapSpec,
        TeamId attackerTeamId,
        TeamId defenderTeamId,
        Turn initialTurn,
        IReadOnlyList<IUnitSpawnSpec> unitSpawns)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Scenario id must not be null or whitespace.", nameof(id))
            : id;
        MapSpec = mapSpec ?? throw new ArgumentNullException(nameof(mapSpec));
        AttackerTeamId = attackerTeamId;
        DefenderTeamId = defenderTeamId;
        InitialTurn = initialTurn;
        UnitSpawns = unitSpawns ?? throw new ArgumentNullException(nameof(unitSpawns));
    }
}
