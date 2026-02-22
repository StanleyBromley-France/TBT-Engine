namespace Core.Engine.Effects.Components.Factories.Creators;

using Core.Engine.Effects.Components.Factories;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
internal sealed class InstantDamageCreator : ComponentInstanceCreatorBase<InstantDamageComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public InstantDamageCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(InstantDamageComponentTemplate template)
    {
        return new InstantDamageComponentInstance(_ids.Create(), template);
    }
}
