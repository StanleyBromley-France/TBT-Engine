namespace Core.Game.Factories.Effects;

using Core.Domain.Effects.Instances.Mutable;
using Core.Game.Session;
using Domain.Types;

internal interface IEffectInstanceFactory
{
    EffectInstance Create(
    EffectTemplateId templateId,
    UnitInstanceId sourceUnitId,
    UnitInstanceId targetUnitId,
    InstanceAllocationState instanceAllocation);
}
