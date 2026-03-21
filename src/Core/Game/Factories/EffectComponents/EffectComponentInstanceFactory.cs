namespace Core.Game.Factories.EffectComponents;

using Core.Game.Factories.EffectComponents.Registry;
using Core.Game.Session;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
public sealed class EffectComponentInstanceFactory : IEffectComponentInstanceFactory
{
    private readonly IComponentInstanceCreatorRegistry _registry;

    public EffectComponentInstanceFactory(
        IComponentInstanceCreatorRegistry registry)
    {
        _registry = registry;
    }

    public EffectComponentInstance Create(
        EffectComponentTemplate componentTemplate, 
        InstanceAllocationState instanceAllocation)
    {
        var creator = _registry.Resolve(componentTemplate);

        return creator.Create(componentTemplate, instanceAllocation);
    }
}
