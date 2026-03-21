namespace Core.Game.Factories.EffectComponents.Creators;

using Core.Game.Factories.EffectComponents;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;

internal sealed class DamageOverTimeCreator : ComponentInstanceCreatorBase<DamageOverTimeComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public DamageOverTimeCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(DamageOverTimeComponentTemplate template)
    {
        return new DamageOverTimeComponentInstance(_ids.Create(), template);
    }
}
