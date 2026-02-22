namespace Core.Engine.Effects.Components.Factories;

using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
internal interface IEffectComponentInstanceFactory
{
    public EffectComponentInstance Create(
    EffectComponentTemplate componentTemplate);
}
