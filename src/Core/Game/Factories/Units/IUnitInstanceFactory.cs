namespace Core.Game.Factories.Units;

using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;
using Core.Game.Session;

public interface IUnitInstanceFactory
{
    UnitInstance Create(UnitTemplate template, TeamId teamId, HexCoord position, InstanceAllocationState instanceAllocation);
}
