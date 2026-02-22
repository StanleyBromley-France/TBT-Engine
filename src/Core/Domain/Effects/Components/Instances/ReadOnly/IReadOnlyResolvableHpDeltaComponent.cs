namespace Core.Domain.Effects.Components.Instances.ReadOnly;

using Components.Instances.Mutable;


public interface IReadOnlyResolvableHpDeltaComponent : IResolvableHpDeltaComponent
{
    HpType HpType { get; }
}
