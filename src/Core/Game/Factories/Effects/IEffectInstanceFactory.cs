namespace Core.Game.Factories.Effects;

using Core.Domain.Effects.Instances.Mutable;
using Core.Game.Requests;
using Core.Game.Session;

internal interface IEffectInstanceFactory
{
    EffectInstance Create(
    CreateEffectRequest effectRequest,
    InstanceAllocationState instanceAllocation);
}
