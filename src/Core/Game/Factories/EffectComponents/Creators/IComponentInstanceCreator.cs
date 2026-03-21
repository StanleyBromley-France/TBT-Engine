namespace Core.Game.Factories.EffectComponents.Creators;

using Core.Game.Session;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
public interface IComponentInstanceCreator
{
    bool CanCreate(EffectComponentTemplate template);
    EffectComponentInstance Create(EffectComponentTemplate template, InstanceAllocationState instanceAllocation);
}

public interface IComponentInstanceCreator<in TTemplate>
    : IComponentInstanceCreator
    where TTemplate : EffectComponentTemplate
{
    EffectComponentInstance Create(TTemplate template, InstanceAllocationState instanceAllocation);
}
