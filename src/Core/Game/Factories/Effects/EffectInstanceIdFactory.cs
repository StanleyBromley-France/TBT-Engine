namespace Core.Game.Factories.Effects;

using Domain.Types;

internal sealed class EffectInstanceIdFactory : IEffectInstanceIdFactory
{
    private int _next = 1;

    public EffectInstanceId Create() => new EffectInstanceId(_next++);
}
