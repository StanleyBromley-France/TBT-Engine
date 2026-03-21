namespace Core.Game.Factories.EffectComponents;

using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
internal interface IEffectComponentInstanceFactory
{
    public EffectComponentInstance Create(
    EffectComponentTemplate componentTemplate);
}
