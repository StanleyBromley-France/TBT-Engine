using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;

namespace Core.Engine.Effects.Components.Factories;

using Core.Engine.Effects.Components.Factories.Registry;
public sealed class EffectComponentInstanceFactory
{
    private readonly IComponentInstanceCreatorRegistry _registry;

    public EffectComponentInstanceFactory(
        IComponentInstanceCreatorRegistry registry)
    {
        _registry = registry;
    }

    public EffectComponentInstance Create(
        EffectComponentTemplate componentTemplate,
        EffectInstance effect)
    {
        if (componentTemplate is null) throw new ArgumentNullException(nameof(componentTemplate));

        var creator = _registry.Resolve(componentTemplate);

        return creator.Create(componentTemplate);
    }
}
