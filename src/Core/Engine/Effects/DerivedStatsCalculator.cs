using Core.Domain.Types;
using Core.Domain.Effects.Stats;
using Core.Game;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Effects.Components.Instances.ReadOnly;

namespace Core.Engine.Effects;

public static class DerivedStatsCalculator
{
    public static UnitDerivedStats Compute(IReadOnlyGameState state, UnitInstanceId unitId)
    {
        var unit = state.UnitInstances[unitId];
        var baseStats = unit.Template.BaseStats;

        // 1) build stats bag from active effects
        var bag = new DerivedStatsModifierBag();

        if (state.ActiveEffects.TryGetValue(unitId, out var effectsById))
        {
            foreach (var effect in effectsById.Values)
            {
                foreach (var component in effect.Components)
                {
                    if (component is IDerivedStatsContributor contributor)
                    {
                        contributor.Contribute(
                            modifierSink: bag,
                            effectId: effect.Id,
                            stacks: effect.CurrentStacks);
                    }
                }
            }
        }

        // 2) compute normal stats
        var movePoints = ApplyStat(baseStats.MovePoints, StatType.MovePoints, bag);
        var armourPoints = ApplyStat(baseStats.ArmourPoints, StatType.ArmourPoints, bag);
        var magicResistance = ApplyStat(baseStats.MagicResistance, StatType.MagicResistance, bag);

        var maxHp = ApplyStat(baseStats.MaxHP, StatType.MaxHP, bag);
        var maxManaPoints = ApplyStat(baseStats.MaxManaPoints, StatType.MaxManaPoints, bag);

        var actionPoints = ApplyStat(baseStats.ActionPoints, StatType.ActionPoints, bag);

        // 3) compute percentage-effectiveness stats (base = 100)
        var healingReceived = ApplyEffectivenessStat(StatType.HealingReceived, bag);
        var healingDealt = ApplyEffectivenessStat(StatType.HealingDealt, bag);
        var damageTaken = ApplyEffectivenessStat(StatType.DamageTaken, bag);
        var damageDealt = ApplyEffectivenessStat(StatType.DamageDealt, bag);

        return new UnitDerivedStats(
            movePoints,
            armourPoints,
            magicResistance,
            maxHp,
            maxManaPoints,
            actionPoints,
            healingReceived,
            healingDealt,
            damageTaken,
            damageDealt
            );
    }

    private static int ApplyStat(int baseValue, StatType stat, IDerivedStatsModifierSource bag)
    {
        var value = baseValue;

        // flats
        value += bag.GetGoodFlatOrZero(stat);
        value += bag.GetBadFlatOrZero(stat);

        // percent-of-base deltas (bag returns decimals: 0.20f = +20%)
        var goodPct = bag.GetGoodPercentOrZero(stat);
        var badPct = bag.GetBadPercentOrZero(stat);

        value += (int)MathF.Round(baseValue * goodPct);
        value += (int)MathF.Round(baseValue * badPct);

        return value;
    }

    private static int ApplyEffectivenessStat(StatType stat, IDerivedStatsModifierSource bag)
    {
        // base effectiveness = 100%
        var value = ApplyStat(100, stat, bag);

        // recommended safety clamp
        if (value < 0) value = 0;

        return value;
    }
}
