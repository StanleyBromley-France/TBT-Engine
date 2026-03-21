namespace Core.Game.Factories.EffectComponents.Creators;

using Core.Game.Factories.EffectComponents;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;

internal sealed class PercentAttributeModifierCreator : ComponentInstanceCreatorBase<PercentAttributeModifierComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public PercentAttributeModifierCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(PercentAttributeModifierComponentTemplate template)
    {
        return new PercentAttributeModifierComponentInstance(_ids.Create(), template);
    }
}
