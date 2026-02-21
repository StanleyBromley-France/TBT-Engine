namespace Core.Engine.Effects.Components.Factories.Creators;

using Core.Engine.Effects.Components.Factories;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;

internal class DamageOverTimeCreator : ComponentInstanceCreatorBase<DamageOverTimeComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public DamageOverTimeCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(DamageOverTimeComponentTemplate template)
    {
        return new DamageOverTimeComponentInstance(_ids.Create(), template);
    }
}
