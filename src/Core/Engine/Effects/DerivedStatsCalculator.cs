using Core.Domain.Types;
using Core.Domain.Effects.Stats;
using Core.Game;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Effects.Components.Instances.ReadOnly;

namespace Core.Engine.Effects;

public class DerivedStatsCalculator
{
    public UnitDerivedStats Compute(IReadOnlyGameState state, UnitInstanceId unitId)
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

        // 2) compute stats
        var movePoints = ApplyStat(baseStats.MovePoints, StatType.MovePoints, bag);

        var maxHp = ApplyStat(baseStats.MaxHP, StatType.MaxHP, bag);
        var maxManaPoints = ApplyStat(baseStats.MaxManaPoints, StatType.MaxManaPoints, bag);

        var actionPoints = ApplyStat(baseStats.ActionPoints, StatType.ActionPoints, bag);

        var physicalDamageReceived = ApplyStat(baseStats.PhysicalDamageReceived, StatType.PhysicalDamageReceived, bag);
        var magicDamageReceived = ApplyStat(baseStats.MagicDamageReceived, StatType.MagicDamageReceived, bag);
        var healingReceived = ApplyStat(baseStats.HealingReceived, StatType.HealingReceived, bag);
        var healingDealt = ApplyStat(baseStats.HealingDealt, StatType.HealingDealt, bag);
        var damageDealt = ApplyStat(baseStats.DamageDealt, StatType.DamageDealt, bag);

        return new UnitDerivedStats(
            movePoints,
            physicalDamageReceived,
            magicDamageReceived,
            maxHp,
            maxManaPoints,
            actionPoints,
            healingReceived,
            healingDealt,
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

        if (value < 0) value = 0;

        return value;
    }
}
