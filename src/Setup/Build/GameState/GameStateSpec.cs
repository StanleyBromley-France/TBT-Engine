namespace Setup.Build.GameState;

using Core.Domain.Types;
using Core.Game.Bootstrap.Contracts;
using Core.Game.State;

public sealed class GameStateSpec : IGameStateSpec
{
    public string Id { get; }
    public IMapSpec MapSpec { get; }
    public TeamId AttackerTeamId { get; }
    public TeamId DefenderTeamId { get; }
    public Turn InitialTurn { get; }
    public RngState InitialRng { get; }
    public IReadOnlyList<IUnitSpawnSpec> UnitSpawns { get; }

    public GameStateSpec(
        string id,
        IMapSpec mapSpec,
        TeamId attackerTeamId,
        TeamId defenderTeamId,
        Turn initialTurn,
        RngState initialRng,
        IReadOnlyList<IUnitSpawnSpec> unitSpawns)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Scenario id must not be null or whitespace.", nameof(id))
            : id;
        MapSpec = mapSpec ?? throw new ArgumentNullException(nameof(mapSpec));
        AttackerTeamId = attackerTeamId;
        DefenderTeamId = defenderTeamId;
        InitialTurn = initialTurn;
        InitialRng = initialRng ?? throw new ArgumentNullException(nameof(initialRng));
        UnitSpawns = unitSpawns ?? throw new ArgumentNullException(nameof(unitSpawns));
    }
}
