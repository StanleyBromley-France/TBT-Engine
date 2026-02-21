namespace Core.Engine.Effects.Components.Factories.Creators;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
public interface IComponentInstanceCreator
{
    bool CanCreate(EffectComponentTemplate template);
    EffectComponentInstance Create(EffectComponentTemplate template);
}

public interface IComponentInstanceCreator<in TTemplate>
    : IComponentInstanceCreator
    where TTemplate : EffectComponentTemplate
{
    EffectComponentInstance Create(TTemplate template);
}
