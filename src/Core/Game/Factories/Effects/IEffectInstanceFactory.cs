namespace Core.Game.Factories.Effects;

using Core.Domain.Effects.Instances.ReadOnly;
using Core.Engine.Mutation;
using Core.Game.Session;
using Domain.Types;

internal interface IEffectInstanceFactory
{
    IReadOnlyEffectInstance Create(
    GameMutationContext context,
    EffectTemplateId templateId,
    UnitInstanceId sourceUnitId,
    UnitInstanceId targetUnitId,
    InstanceAllocationState instanceAllocation);
}
