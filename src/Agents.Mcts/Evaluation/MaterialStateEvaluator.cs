namespace Agents.Mcts.Evaluation;

using Core.Domain.Types;
using Core.Game.Match;
using Core.Game.State.ReadOnly;

public sealed class MaterialStateEvaluator : IStateEvaluator
{
    public double Evaluate(IReadOnlyGameState state, StateEvaluationContext context)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        var perspective = context.Perspective;
        var profile = context.Profile ?? throw new ArgumentNullException(nameof(context.Profile));

        var allyAliveCount = 0;
        var enemyAliveCount = 0;
        var allyHp = 0d;
        var enemyHp = 0d;
        var allyMana = 0d;
        var enemyMana = 0d;

        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive)
                continue;

            if (unit.Team == perspective)
            {
                allyAliveCount++;
                allyHp += unit.Resources.HP;
                allyMana += unit.Resources.Mana;
            }
            else
            {
                enemyAliveCount++;
                enemyHp += unit.Resources.HP;
                enemyMana += unit.Resources.Mana;
            }
        }

        if (context.Outcome.Type == GameOutcomeType.Draw)
            return 0d;

        if (context.Outcome.Type == GameOutcomeType.Victory)
        {
            return context.Outcome.WinningTeam == perspective
                ? profile.WinScore + allyHp + allyAliveCount * profile.AllyUnitWeight
                : -profile.WinScore - enemyHp - enemyAliveCount * profile.EnemyUnitWeight;
        }

        if (allyAliveCount == 0 && enemyAliveCount == 0)
            return 0d;

        if (enemyAliveCount == 0)
            return profile.WinScore + allyHp + allyAliveCount * profile.AllyUnitWeight;

        if (allyAliveCount == 0)
            return -profile.WinScore - enemyHp - enemyAliveCount * profile.EnemyUnitWeight;

        var opposingTeam = GetOpposingPerspective(state, perspective);

        var score =
            allyAliveCount * profile.AllyUnitWeight -
            enemyAliveCount * profile.EnemyUnitWeight +
            allyHp * profile.AllyHpWeight -
            enemyHp * profile.EnemyHpWeight +
            ComputeResourceCapacityScore(state, perspective) * profile.AllyResourceCapacityWeight -
            ComputeResourceCapacityScore(state, opposingTeam) * profile.EnemyResourceCapacityWeight +
            ComputeDerivedCombatScore(state, perspective) * profile.AllyDerivedCombatWeight -
            ComputeDerivedCombatScore(state, opposingTeam) * profile.EnemyDerivedCombatWeight +
            allyMana * profile.AllyManaWeight -
            enemyMana * profile.EnemyManaWeight;

        var attackerTurnsTaken = state.Turn.AttackerTurnsTaken;
        var remainingAttackerTurns = Math.Max(0, context.MaxAttackerTurns - attackerTurnsTaken);

        score += remainingAttackerTurns * profile.RemainingAttackerTurnsWeight + attackerTurnsTaken * profile.ElapsedAttackerTurnsWeight;

        return score;
    }

    private static TeamId GetOpposingPerspective(IReadOnlyGameState state, TeamId perspective)
    {
        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive)
                continue;

            if (unit.Team != perspective)
                return unit.Team;
        }

        return perspective;
    }

    private static double ComputeResourceCapacityScore(IReadOnlyGameState state, TeamId team)
    {
        var total = 0d;

        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive || unit.Team != team)
                continue;

            var baseStats = unit.Template.BaseStats;
            var derived = unit.DerivedStats;

            total += GetNormalizedDelta(derived.MaxHP, baseStats.MaxHP);
            total += GetNormalizedDelta(derived.MaxManaPoints, baseStats.MaxManaPoints);
            total += GetNormalizedDelta(derived.MaxMovePoints, baseStats.MovePoints);
            total += GetNormalizedDelta(derived.MaxActionPoints, baseStats.ActionPoints);
        }

        return total;
    }

    private static double ComputeDerivedCombatScore(IReadOnlyGameState state, TeamId team)
    {
        var total = 0d;

        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive || unit.Team != team)
                continue;

            var baseStats = unit.Template.BaseStats;
            var derived = unit.DerivedStats;

            total += GetNormalizedDelta(derived.DamageDealt, baseStats.DamageDealt);
            total += GetNormalizedDelta(derived.HealingDealt, baseStats.HealingDealt);
            total += GetNormalizedDelta(derived.HealingReceived, baseStats.HealingReceived);
            total += GetInverseNormalizedDelta(derived.PhysicalDamageReceived, baseStats.PhysicalDamageReceived);
            total += GetInverseNormalizedDelta(derived.MagicDamageReceived, baseStats.MagicDamageReceived);
        }

        return total;
    }

    private static double GetNormalizedDelta(int currentValue, int baseValue)
    {
        if (baseValue <= 0)
            return 0d;

        return (currentValue - baseValue) / (double)baseValue;
    }

    private static double GetInverseNormalizedDelta(int currentValue, int baseValue)
    {
        if (baseValue <= 0)
            return 0d;

        return (baseValue - currentValue) / (double)baseValue;
    }
}
