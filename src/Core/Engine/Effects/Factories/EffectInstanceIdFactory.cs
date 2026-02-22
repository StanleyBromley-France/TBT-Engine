namespace Core.Engine.Effects.Factories;

using Domain.Types;

internal sealed class EffectInstanceIdFactory : IEffectInstanceIdFactory
{
    private int _next = 1;

    public EffectInstanceId Create() => new EffectInstanceId(_next++);
}
