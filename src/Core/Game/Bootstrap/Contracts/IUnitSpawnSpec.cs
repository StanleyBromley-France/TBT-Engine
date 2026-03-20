namespace Core.Game.Bootstrap.Contracts;

using Core.Domain.Types;

public interface IUnitSpawnSpec
{
    UnitTemplateId UnitTemplateId { get; }
    TeamId TeamId { get; }
    HexCoord Position { get; }
}
