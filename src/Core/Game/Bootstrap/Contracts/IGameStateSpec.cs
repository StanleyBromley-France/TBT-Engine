namespace Core.Game.Bootstrap.Contracts;

using Core.Domain.Types;
using Core.Game.State;

public interface IGameStateSpec
{
    string Id { get; }
    IMapSpec MapSpec { get; }
    TeamId AttackerTeamId { get; }
    TeamId DefenderTeamId { get; }
    Turn InitialTurn { get; }
    RngState InitialRng { get; }
    IReadOnlyList<IUnitSpawnSpec> UnitSpawns { get; }
}
