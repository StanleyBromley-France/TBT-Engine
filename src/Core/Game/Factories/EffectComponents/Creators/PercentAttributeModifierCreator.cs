namespace Core.Game.Factories.EffectComponents.Creators;

using Core.Game.Factories.EffectComponents;
using Core.Game.Session;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;

internal sealed class PercentAttributeModifierCreator : ComponentInstanceCreatorBase<PercentAttributeModifierComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public PercentAttributeModifierCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(PercentAttributeModifierComponentTemplate template, InstanceAllocationState instanceAllocation)
    {
        return new PercentAttributeModifierComponentInstance(_ids.Create(instanceAllocation), template);
    }
}
