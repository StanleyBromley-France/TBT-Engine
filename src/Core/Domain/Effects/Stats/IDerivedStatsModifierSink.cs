using Core.Domain.Types;

namespace Core.Domain.Effects.Stats;

public interface IDerivedStatsModifierSink
{
    void ConsiderFlat(StatType stat, int delta, EffectInstanceId effectId);
    void ConsiderPercent(StatType stat, int percentAdd, EffectInstanceId effectId);
}
