namespace Core.Domain.Effects.Components.Instances.ReadOnly;

using Components.Templates;
using Types;

public interface IReadOnlyEffectComponentInstance
{
    EffectComponentInstanceId Id { get; }
    EffectComponentTemplate Template { get; }
}