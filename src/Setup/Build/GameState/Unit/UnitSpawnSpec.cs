namespace Setup.Build.GameState.Unit;

using Core.Game.Bootstrap.Contracts;
using Core.Domain.Types;

public sealed class UnitSpawnSpec : IUnitSpawnSpec
{
    public UnitTemplateId UnitTemplateId { get; }
    public TeamId TeamId { get; }
    public HexCoord Position { get; }

    public UnitSpawnSpec(
        UnitTemplateId unitTemplateId,
        TeamId teamId,
        HexCoord position)
    {
        UnitTemplateId = unitTemplateId;
        TeamId = teamId;
        Position = position;
    }
}
