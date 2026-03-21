namespace Core.Game.Factories.Effects;

using Core.Game.Session;
using Domain.Types;

internal sealed class EffectInstanceIdFactory : IEffectInstanceIdFactory
{
    public EffectInstanceId Create(InstanceAllocationState instanceAllocation) => new EffectInstanceId(instanceAllocation.GetNextEffectInstanceIdSeed());
}
