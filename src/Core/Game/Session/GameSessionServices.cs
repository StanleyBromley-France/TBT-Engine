namespace Core.Game.Session;

using Core.Game.Factories.EffectComponents;
using Core.Game.Factories.Effects;
using Core.Game.Factories.Units;

internal sealed class GameSessionServices
{
    public IUnitInstanceFactory Units { get; }
    public IEffectInstanceFactory Effects { get; }
    public IEffectComponentInstanceFactory EffectComponents { get; }

    public GameSessionServices(
        IUnitInstanceFactory units,
        IEffectInstanceFactory effects,
        IEffectComponentInstanceFactory effectComponents)
    {
        Units = units ?? throw new ArgumentNullException(nameof(units));
        Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        EffectComponents = effectComponents ?? throw new ArgumentNullException(nameof(effectComponents));
    }
}
