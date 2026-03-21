namespace Core.Game.Factories.Effects;

using Core.Game.Session;
using Domain.Types;

internal interface IEffectInstanceIdFactory
{
    EffectInstanceId Create(InstanceAllocationState instanceAllocation);
}
