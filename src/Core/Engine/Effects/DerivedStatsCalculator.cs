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

        var baseArmor = unit.Template.BaseStats.ArmourPoints;
        var baseMr = unit.Template.BaseStats.MagicResistance;

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
                            modifierSink: bag, // uses sink interface
                            effectId: effect.Id,
                            stacks: effect.CurrentStacks);
                    }
                }
            }
        }

        // 2) apply
        var armor = ApplyStat(baseArmor, StatType.ArmourPoints, bag);
        var mr = ApplyStat(baseMr, StatType.MagicResistance, bag);

        return new UnitDerivedStats(mr, armor);
    }

    private static int ApplyStat(int baseValue, StatType stat, IDerivedStatsModifierSource bag)
    {
        var value = baseValue;

        // flats
        value += bag.GetGoodFlatOrZero(stat);
        value += bag.GetBadFlatOrZero(stat);

        // percentages converted to flat deltas from base
        var goodPct = bag.GetGoodPercentOrZero(stat);
        var badPct = bag.GetBadPercentOrZero(stat);

        value += (int)MathF.Round(baseValue * goodPct);
        value += (int)MathF.Round(baseValue * badPct);

        return value;
    }
}
