namespace Core.Engine.Effects.Components.Factories.Creators;

using Core.Engine.Effects.Components.Factories;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;

internal sealed class FlatAttributeModifierCreator : ComponentInstanceCreatorBase<FlatAttributeModifierComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public FlatAttributeModifierCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(FlatAttributeModifierComponentTemplate template)
    {
        return new FlatAttributeModifierComponentInstance(_ids.Create(), template);
    }
}
