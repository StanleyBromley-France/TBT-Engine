namespace Core.Engine.Effects.Components.Factories.Creators;

using Core.Engine.Effects.Components.Factories;
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
