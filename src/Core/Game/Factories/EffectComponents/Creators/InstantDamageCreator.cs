namespace Core.Game.Factories.EffectComponents.Creators;

using Core.Game.Factories.EffectComponents;
using Core.Game.Session;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
internal sealed class InstantDamageCreator : ComponentInstanceCreatorBase<InstantDamageComponentTemplate>
{
    private readonly IEffectComponentInstanceIdFactory _ids;

    public InstantDamageCreator(IEffectComponentInstanceIdFactory ids) => _ids = ids;

    public override EffectComponentInstance Create(InstantDamageComponentTemplate template, InstanceAllocationState instanceAllocation)
    {
        return new InstantDamageComponentInstance(_ids.Create(instanceAllocation), template);
    }
}
