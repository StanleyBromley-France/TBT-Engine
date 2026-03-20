namespace Core.Game.Factories.Units;

using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;

public interface IUnitInstanceFactory
{
    UnitInstance Create(UnitTemplate template, TeamId teamId, HexCoord position);
}
