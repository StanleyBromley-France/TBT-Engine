using Core.Domain.Types;

namespace Core.Domain.Effects.Stats;

/// <summary>
/// <para>
/// Collects dominant effect contributions for DerivedStats recomputation.
/// </para>
/// <para>
/// For each <see cref="StatType"/>, stores the strongest buff and strongest debuff
/// for flat deltas and percent-of-base deltas (0.10f = +10%, -0.10f = -10%).
/// </para>
/// <para>
/// Contributions do not stack across EffectInstances. Buffs use MAX dominance,
/// debuffs use MIN dominance, with deterministic tie-breaking by
/// <see cref="EffectInstanceId"/>.
/// </para>
/// </summary>

public sealed class DerivedStatsModifierBag : IDerivedStatsModifierSink, IDerivedStatsModifierSource
{
    private struct FlatEntry
    {
        public int Value;
        public EffectInstanceId Winner;
        public bool HasValue;
    }

    private struct PercentEntry
    {
        public float Value;
        public EffectInstanceId Winner;
        public bool HasValue;
    }

    // buffs
    private readonly Dictionary<StatType, PercentEntry> _goodPercent = new();
    private readonly Dictionary<StatType, FlatEntry> _goodFlat = new();

    // debuffs
    private readonly Dictionary<StatType, PercentEntry> _badPercent = new();
    private readonly Dictionary<StatType, FlatEntry> _badFlat = new();

    // sink (contributors call)

    public void ConsiderFlat(StatType stat, int delta, EffectInstanceId effectId)
    {
        if (delta == 0) return;

        if (delta > 0)
            ConsiderFlatMax(_goodFlat, stat, delta, effectId);
        else
            ConsiderFlatMin(_badFlat, stat, delta, effectId);
    }

    public void ConsiderPercent(StatType stat, float percentAdd, EffectInstanceId effectId)
    {
        if (percentAdd == 0f) return;

        if (percentAdd > 0f)
            ConsiderPercentMax(_goodPercent, stat, percentAdd, effectId);
        else
            ConsiderPercentMin(_badPercent, stat, percentAdd, effectId);
    }

    // source (calculator calls)

    public int GetGoodFlatOrZero(StatType stat)
        => _goodFlat.TryGetValue(stat, out var e) && e.HasValue ? e.Value : 0;

    public int GetBadFlatOrZero(StatType stat)
        => _badFlat.TryGetValue(stat, out var e) && e.HasValue ? e.Value : 0;

    public float GetGoodPercentOrZero(StatType stat)
        => _goodPercent.TryGetValue(stat, out var e) && e.HasValue ? e.Value : 0f;

    public float GetBadPercentOrZero(StatType stat)
        => _badPercent.TryGetValue(stat, out var e) && e.HasValue ? e.Value : 0f;

    // internal

    private static void ConsiderFlatMax(
        Dictionary<StatType, FlatEntry> dict,
        StatType stat,
        int value,
        EffectInstanceId effectId)
    {
        if (!dict.TryGetValue(stat, out var ex) || !ex.HasValue)
        {
            dict[stat] = new FlatEntry { Value = value, Winner = effectId, HasValue = true };
            return;
        }

        if (value > ex.Value || (value == ex.Value && IsLower(effectId, ex.Winner)))
            dict[stat] = new FlatEntry { Value = value, Winner = effectId, HasValue = true };
    }

    private static void ConsiderFlatMin(
        Dictionary<StatType, FlatEntry> dict,
        StatType stat,
        int value,
        EffectInstanceId effectId)
    {
        if (!dict.TryGetValue(stat, out var ex) || !ex.HasValue)
        {
            dict[stat] = new FlatEntry { Value = value, Winner = effectId, HasValue = true };
            return;
        }

        if (value < ex.Value || (value == ex.Value && IsLower(effectId, ex.Winner)))
            dict[stat] = new FlatEntry { Value = value, Winner = effectId, HasValue = true };
    }

    private static void ConsiderPercentMax(
        Dictionary<StatType, PercentEntry> dict,
        StatType stat,
        float value,
        EffectInstanceId effectId)
    {
        if (!dict.TryGetValue(stat, out var ex) || !ex.HasValue)
        {
            dict[stat] = new PercentEntry { Value = value, Winner = effectId, HasValue = true };
            return;
        }

        if (value > ex.Value || (value == ex.Value && IsLower(effectId, ex.Winner)))
            dict[stat] = new PercentEntry { Value = value, Winner = effectId, HasValue = true };
    }

    private static void ConsiderPercentMin(
        Dictionary<StatType, PercentEntry> dict,
        StatType stat,
        float value,
        EffectInstanceId effectId)
    {
        if (!dict.TryGetValue(stat, out var ex) || !ex.HasValue)
        {
            dict[stat] = new PercentEntry { Value = value, Winner = effectId, HasValue = true };
            return;
        }

        if (value < ex.Value || (value == ex.Value && IsLower(effectId, ex.Winner)))
            dict[stat] = new PercentEntry { Value = value, Winner = effectId, HasValue = true };
    }


    // in cases where flat/percentage value is equal, tie is decided through effectinstanceid
    private static bool IsLower(EffectInstanceId a, EffectInstanceId b)
    {
        return a.Value < b.Value;
    }
}
