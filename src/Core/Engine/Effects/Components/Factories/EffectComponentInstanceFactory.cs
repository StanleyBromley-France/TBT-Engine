namespace Core.Engine.Effects.Components.Factories;

using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
using Effects.Components.Factories.Registry;
public sealed class EffectComponentInstanceFactory : IEffectComponentInstanceFactory
{
    private readonly IComponentInstanceCreatorRegistry _registry;

    public EffectComponentInstanceFactory(
        IComponentInstanceCreatorRegistry registry)
    {
        _registry = registry;
    }

    public EffectComponentInstance Create(
        EffectComponentTemplate componentTemplate)
    {
        if (componentTemplate is null) throw new ArgumentNullException(nameof(componentTemplate));

        var creator = _registry.Resolve(componentTemplate);

        return creator.Create(componentTemplate);
    }
}
