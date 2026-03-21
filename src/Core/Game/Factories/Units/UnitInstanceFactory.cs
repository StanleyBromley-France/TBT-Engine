namespace Core.Game.Factories.Units;

using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;
using Core.Game.Session;

public sealed class UnitInstanceFactory : IUnitInstanceFactory
{
    private readonly IUnitInstanceIdFactory _ids;

    public UnitInstanceFactory(IUnitInstanceIdFactory ids)
    {
        _ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public UnitInstance Create(UnitTemplate template, TeamId teamId, HexCoord position, InstanceAllocationState instanceAllocation)
    {

        return new UnitInstance(
            id: _ids.Create(instanceAllocation),
            team: teamId,
            template: template,
            position: position);
    }
}
