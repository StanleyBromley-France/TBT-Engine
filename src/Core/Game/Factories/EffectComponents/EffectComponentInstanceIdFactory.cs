using Core.Domain.Types;

namespace Core.Game.Factories.EffectComponents;

public sealed class EffectComponentInstanceIdFactory : IEffectComponentInstanceIdFactory
{
    private int _next = 1;

    public EffectComponentInstanceId Create() => new EffectComponentInstanceId(_next++);
}
