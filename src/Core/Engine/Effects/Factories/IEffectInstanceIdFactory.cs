namespace Core.Engine.Effects.Factories;

using Domain.Types;

internal interface IEffectInstanceIdFactory
{
    EffectInstanceId Create();
}
