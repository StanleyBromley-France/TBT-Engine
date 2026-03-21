using Core.Domain.Types;
using Core.Game.Session;

namespace Core.Game.Factories.EffectComponents;

public sealed class EffectComponentInstanceIdFactory : IEffectComponentInstanceIdFactory
{
    public EffectComponentInstanceId Create(InstanceAllocationState instanceAllocation) => 
        new EffectComponentInstanceId(instanceAllocation.GetNextEffectComponentInstanceIdSeed());
}
