namespace Core.Game.Factories.EffectComponents.Registry;

using Core.Domain.Effects.Components.Templates;
using Core.Game.Factories.EffectComponents.Creators;

public interface IComponentInstanceCreatorRegistry
{
    IComponentInstanceCreator Resolve(EffectComponentTemplate template);
}