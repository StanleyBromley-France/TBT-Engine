namespace Core.Engine.Effects.Factories;

using Domain.Effects.Instances.Mutable;
using Domain.Types;

internal interface IEffectInstanceFactory
{
    EffectInstance Create(
    EffectTemplateId templateId,
    UnitInstanceId sourceUnitId,
    UnitInstanceId targetUnitId);
}
