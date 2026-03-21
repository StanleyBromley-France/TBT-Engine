namespace Core.Game.Session;

using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Units.Instances.Mutable;
using Core.Game.Factories.Effects;
using Core.Game.Factories.Units;
using Core.Game.Requests;

internal sealed class GameSessionServices
{
    private readonly IUnitInstanceFactory _units;
    private readonly IEffectInstanceFactory _effects;

    public GameSessionServices(
        IUnitInstanceFactory units,
        IEffectInstanceFactory effects)
    {
        _units = units ?? throw new ArgumentNullException(nameof(units));
        _effects = effects ?? throw new ArgumentNullException(nameof(effects));
    }

    internal EffectInstance CreateEffect(CreateEffectRequest request, InstanceAllocationState instanceAllocationState) => _effects.Create(request, instanceAllocationState);
    internal UnitInstance CreateUnit(SpawnUnitRequest request, InstanceAllocationState instanceAllocationState) => _units.Create(request, instanceAllocationState);
}
