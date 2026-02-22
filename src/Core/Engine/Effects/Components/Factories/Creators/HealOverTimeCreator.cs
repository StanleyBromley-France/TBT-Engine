namespace Core.Engine.Effects.Components.Factories.Creators;

using Core.Engine.Effects.Components.Factories;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
internal sealed class HealOverTimeCreator : ComponentInstanceCreatorBase<HealOverTimeComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public HealOverTimeCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(HealOverTimeComponentTemplate template)
    {
        return new HealOverTimeComponentInstance(_ids.Create(), template);
    }
}
