namespace Core.Game.Factories.EffectComponents;

using Core.Game.Session;
using Domain.Types;

public interface IEffectComponentInstanceIdFactory
{
    EffectComponentInstanceId Create(InstanceAllocationState instanceAllocation);
}
