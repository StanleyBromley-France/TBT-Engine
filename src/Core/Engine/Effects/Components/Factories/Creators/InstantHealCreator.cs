namespace Core.Engine.Effects.Components.Factories.Creators;

using Core.Engine.Effects.Components.Factories;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;

internal sealed class InstantHealCreator : ComponentInstanceCreatorBase<InstantHealComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public InstantHealCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(InstantHealComponentTemplate template)
    {
        return new InstantHealComponentInstance(_ids.Create(), template);
    }
}
