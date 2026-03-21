namespace Core.Game.Factories.Units;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;
using Core.Game.Requests;
using Core.Game.Session;

public sealed class UnitInstanceFactory : IUnitInstanceFactory
{
    private readonly IUnitInstanceIdFactory _ids;
    private readonly IUnitTemplateRepository _templateRepository;
    public UnitInstanceFactory(IUnitInstanceIdFactory ids, IUnitTemplateRepository templateRepository)
    {
        _ids = ids ?? throw new ArgumentNullException(nameof(ids));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
    }

    public UnitInstance Create(SpawnUnitRequest unitRequest, InstanceAllocationState instanceAllocation)
    {
        var template = _templateRepository.Get(unitRequest.UnitTemplateId);
        return new UnitInstance(
            id: _ids.Create(instanceAllocation),
            team: unitRequest.TeamId,
            template: template,
            position: unitRequest.Position);
    }
}
