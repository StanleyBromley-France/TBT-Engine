namespace Core.Game.Factories.EffectComponents;

using Core.Game.Session;
using Domain.Effects.Components.Instances.Mutable;
using Domain.Effects.Components.Templates;
internal interface IEffectComponentInstanceFactory
{
    public EffectComponentInstance Create(
    EffectComponentTemplate componentTemplate,
    InstanceAllocationState instanceAllocation);
}
