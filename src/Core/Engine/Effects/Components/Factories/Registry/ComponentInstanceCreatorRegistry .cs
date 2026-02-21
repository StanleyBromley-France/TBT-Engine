namespace Core.Engine.Effects.Components.Factories.Registry;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Effects.Components.Factories.Creators;

public sealed class ComponentInstanceCreatorRegistry : IComponentInstanceCreatorRegistry
{
    private readonly IReadOnlyList<IComponentInstanceCreator> _creators;

    public ComponentInstanceCreatorRegistry(IReadOnlyList<IComponentInstanceCreator> creators)
    {
        _creators = creators ?? throw new ArgumentNullException(nameof(creators));
    }

    public IComponentInstanceCreator Resolve(EffectComponentTemplate template)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));

        foreach (var creator in _creators)
        {
            if (creator.CanCreate(template))
                return creator;
        }

        throw new NotSupportedException(
            $"No component instance creator registered for template type: {template.GetType().Name}");
    }
}