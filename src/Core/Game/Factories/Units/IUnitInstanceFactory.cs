namespace Core.Game.Factories.Units;

using Core.Domain.Units.Instances.Mutable;
using Core.Game.Requests;
using Core.Game.Session;

public interface IUnitInstanceFactory
{
    UnitInstance Create(SpawnUnitRequest unitRequest, InstanceAllocationState instanceAllocation);
}
