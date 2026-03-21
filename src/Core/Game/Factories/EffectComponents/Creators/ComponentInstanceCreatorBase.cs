namespace Core.Game.Factories.EffectComponents.Creators;

using Core.Game.Session;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
public abstract class ComponentInstanceCreatorBase<TTemplate> : IComponentInstanceCreator<TTemplate>
    where TTemplate : EffectComponentTemplate
{
    public bool CanCreate(EffectComponentTemplate template)
        => template is TTemplate;

    public EffectComponentInstance Create(EffectComponentTemplate template, InstanceAllocationState instanceAllocation)
    {
        if (template is not TTemplate typed)
            throw new ArgumentException(
                $"Expected template of type {typeof(TTemplate).Name}, " +
                $"but got {template.GetType().Name}");

        return Create(typed, instanceAllocation);
    }

    public abstract EffectComponentInstance Create(TTemplate template, InstanceAllocationState instanceAllocation);
}
