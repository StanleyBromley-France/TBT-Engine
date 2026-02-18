using Core.Domain.Effects.Stats;
using Core.Domain.Types;

namespace Core.Domain.Effects.Components.Instances.ReadOnly;

/// <summary>
/// Implemented by effect component instances that contribute
/// to DerivedStats computation.
/// </summary>
public interface IDerivedStatsContributor
{
    void Contribute(
        IDerivedStatsModifierSink modifierSink,
        EffectInstanceId effectId,
        int stacks);
}
