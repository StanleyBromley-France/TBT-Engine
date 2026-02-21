namespace Core.Engine.Effects.Components.Factories.Registry;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Effects.Components.Factories.Creators;

public interface IComponentInstanceCreatorRegistry
{
    IComponentInstanceCreator Resolve(EffectComponentTemplate template);
}