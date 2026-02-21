using Core.Domain.Types;

namespace Core.Engine.Effects.Components.Factories;

public sealed class EffectComponentInstanceIdFactory : IEffectComponentInstanceIdFactory
{
    private int _next = 1;

    public EffectComponentInstanceId Create() => new EffectComponentInstanceId(_next++);
}
