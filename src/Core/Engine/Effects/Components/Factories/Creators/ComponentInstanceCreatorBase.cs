namespace Core.Engine.Effects.Components.Factories.Creators;

using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
public abstract class ComponentInstanceCreatorBase<TTemplate> : IComponentInstanceCreator<TTemplate>
    where TTemplate : EffectComponentTemplate
{
    public bool CanCreate(EffectComponentTemplate template)
        => template is TTemplate;

    public EffectComponentInstance Create(EffectComponentTemplate template)
    {
        if (template is not TTemplate typed)
            throw new ArgumentException(
                $"Expected template of type {typeof(TTemplate).Name}, " +
                $"but got {template.GetType().Name}");

        return Create(typed);
    }

    public abstract EffectComponentInstance Create(TTemplate template);
}
