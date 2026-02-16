namespace Core.Domain.Effects.Instances.ReadOnly;

using Core.Domain.Effects.Components.Instances.ReadOnly;
using Core.Domain.Effects.Templates;
using Core.Domain.Types;

public interface IReadOnlyEffectInstance
{
    EffectInstanceId Id { get; }
    EffectTemplate Template { get; }
    UnitInstanceId SourceUnitId { get; }
    UnitInstanceId[] TargetUnitIds { get; }
    int RemainingTicks { get; }
    int CurrentStacks { get; }
    IReadOnlyEffectComponentInstance[] Components { get; }

    bool IsExpired();
}
