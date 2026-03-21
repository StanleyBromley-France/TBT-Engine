namespace Core.Game.Requests;

using Core.Domain.Types;

public sealed class SpawnUnitRequest
{
    public UnitTemplateId UnitTemplateId { get; }
    public TeamId TeamId { get; }
    public HexCoord Position { get; }

    public SpawnUnitRequest(
        UnitTemplateId unitTemplateId,
        TeamId teamId,
        HexCoord position)
    {
        UnitTemplateId = unitTemplateId;
        TeamId = teamId;
        Position = position;
    }
}
