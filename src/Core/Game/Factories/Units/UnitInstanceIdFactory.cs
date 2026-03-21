namespace Core.Game.Factories.Units;

using Core.Domain.Types;
using Core.Game.Session;

public sealed class UnitInstanceIdFactory : IUnitInstanceIdFactory
{
    public UnitInstanceId Create(InstanceAllocationState instanceAllocation) => new UnitInstanceId(instanceAllocation.GetNextUnitInstanceIdSeed());
}
