namespace Core.Game.Factories.Units;

using Core.Domain.Types;
using Core.Game.Session;

public interface IUnitInstanceIdFactory
{
    UnitInstanceId Create(InstanceAllocationState instanceAllocation);
}
