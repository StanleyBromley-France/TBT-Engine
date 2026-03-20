namespace Core.Game.Bootstrap.Contracts;

using Core.Domain.Types;

public interface IGameStateSpec
{
    string Id { get; }
    IMapSpec MapSpec { get; }
    TeamId AttackerTeamId { get; }
    TeamId DefenderTeamId { get; }
    Turn InitialTurn { get; }
    IReadOnlyList<IUnitSpawnSpec> UnitSpawns { get; }
}
