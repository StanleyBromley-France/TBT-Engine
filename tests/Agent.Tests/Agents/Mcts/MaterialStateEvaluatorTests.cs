using Agents.Mcts.Config;
using Agents.Mcts.Evaluation;
using Agent.Tests.Engine.TestSupport;
using Core.Domain.Types;
using Core.Game.Match;
using Core.Game.State;

namespace Agent.Tests.Agents.Mcts;

public sealed class MaterialStateEvaluatorTests
{
    private static readonly TeamId AttackerTeam = new(1);
    private static readonly TeamId DefenderTeam = new(2);

    [Fact]
    public void Evaluate_AttackerPerspective_Prefers_Faster_Progress()
    {
        var evaluator = new MaterialStateEvaluator();
        var earlierState = CreateState(attackerTurnsTaken: 1, allyHp: 10, enemyHp: 10);
        var laterState = CreateState(attackerTurnsTaken: 5, allyHp: 10, enemyHp: 10);
        var context = new StateEvaluationContext(
            perspective: AttackerTeam,
            maxAttackerTurns: 12,
            outcome: GameOutcome.Ongoing(),
            profile: MctsAgentProfile.Offensive("Attacker"));

        var earlierScore = evaluator.Evaluate(earlierState, context);
        var laterScore = evaluator.Evaluate(laterState, context);

        Assert.True(
            earlierScore > laterScore,
            $"Expected earlier attacker state to score higher. Earlier={earlierScore}, Later={laterScore}");
    }

    [Fact]
    public void Evaluate_DefenderPerspective_Prefers_Stalling()
    {
        var evaluator = new MaterialStateEvaluator();
        var earlierState = CreateState(attackerTurnsTaken: 1, allyHp: 10, enemyHp: 10);
        var laterState = CreateState(attackerTurnsTaken: 5, allyHp: 10, enemyHp: 10);
        var context = new StateEvaluationContext(
            perspective: DefenderTeam,
            maxAttackerTurns: 12,
            outcome: GameOutcome.Ongoing(),
            profile: MctsAgentProfile.Defensive("Defender"));

        var earlierScore = evaluator.Evaluate(earlierState, context);
        var laterScore = evaluator.Evaluate(laterState, context);

        Assert.True(
            laterScore > earlierScore,
            $"Expected later defender state to score higher. Earlier={earlierScore}, Later={laterScore}");
    }

    [Fact]
    public void Evaluate_ProfileChoice_Changes_How_Hp_Trades_Are_Valued()
    {
        var evaluator = new MaterialStateEvaluator();
        var riskyState = CreateState(attackerTurnsTaken: 2, allyHp: 5, enemyHp: 1);
        var safeState = CreateState(attackerTurnsTaken: 2, allyHp: 9, enemyHp: 5);

        var offensiveContext = new StateEvaluationContext(
            perspective: AttackerTeam,
            maxAttackerTurns: 12,
            outcome: GameOutcome.Ongoing(),
            profile: MctsAgentProfile.Offensive());

        var defensiveContext = new StateEvaluationContext(
            perspective: AttackerTeam,
            maxAttackerTurns: 12,
            outcome: GameOutcome.Ongoing(),
            profile: MctsAgentProfile.Defensive());

        var offensiveRisky = evaluator.Evaluate(riskyState, offensiveContext);
        var offensiveSafe = evaluator.Evaluate(safeState, offensiveContext);
        var defensiveRisky = evaluator.Evaluate(riskyState, defensiveContext);
        var defensiveSafe = evaluator.Evaluate(safeState, defensiveContext);

        Assert.True(
            offensiveRisky > offensiveSafe,
            $"Expected offensive profile to prefer risky pressure line. Risky={offensiveRisky}, Safe={offensiveSafe}");

        Assert.True(
            defensiveSafe > defensiveRisky,
            $"Expected defensive profile to prefer safer line. Safe={defensiveSafe}, Risky={defensiveRisky}");
    }

    [Fact]
    public void Evaluate_Prefers_Allied_DerivedStat_Buffs()
    {
        var evaluator = new MaterialStateEvaluator();
        var baselineState = CreateState(attackerTurnsTaken: 2, allyHp: 10, enemyHp: 10);
        var buffedState = CreateState(attackerTurnsTaken: 2, allyHp: 10, enemyHp: 10);
        var buffedAttacker = buffedState.UnitInstances[new UnitInstanceId(1)];
        buffedAttacker.DerivedStats.MaxHP = 14;
        buffedAttacker.DerivedStats.MaxManaPoints = 14;
        buffedAttacker.DerivedStats.DamageDealt = 130;
        buffedAttacker.DerivedStats.PhysicalDamageReceived = 80;
        buffedAttacker.DerivedStats.MagicDamageReceived = 80;

        var context = new StateEvaluationContext(
            perspective: AttackerTeam,
            maxAttackerTurns: 12,
            outcome: GameOutcome.Ongoing(),
            profile: MctsAgentProfile.Balanced());

        var baselineScore = evaluator.Evaluate(baselineState, context);
        var buffedScore = evaluator.Evaluate(buffedState, context);

        Assert.True(
            buffedScore > baselineScore,
            $"Expected allied derived-stat buffs to improve score. Baseline={baselineScore}, Buffed={buffedScore}");
    }

    [Fact]
    public void Evaluate_Penalizes_Enemy_DerivedStat_Buffs()
    {
        var evaluator = new MaterialStateEvaluator();
        var baselineState = CreateState(attackerTurnsTaken: 2, allyHp: 10, enemyHp: 10);
        var buffedEnemyState = CreateState(attackerTurnsTaken: 2, allyHp: 10, enemyHp: 10);
        var buffedDefender = buffedEnemyState.UnitInstances[new UnitInstanceId(2)];
        buffedDefender.DerivedStats.MaxHP = 14;
        buffedDefender.DerivedStats.MaxManaPoints = 14;
        buffedDefender.DerivedStats.DamageDealt = 130;
        buffedDefender.DerivedStats.PhysicalDamageReceived = 80;
        buffedDefender.DerivedStats.MagicDamageReceived = 80;

        var context = new StateEvaluationContext(
            perspective: AttackerTeam,
            maxAttackerTurns: 12,
            outcome: GameOutcome.Ongoing(),
            profile: MctsAgentProfile.Balanced());

        var baselineScore = evaluator.Evaluate(baselineState, context);
        var buffedEnemyScore = evaluator.Evaluate(buffedEnemyState, context);

        Assert.True(
            buffedEnemyScore < baselineScore,
            $"Expected enemy derived-stat buffs to reduce score. Baseline={baselineScore}, EnemyBuffed={buffedEnemyScore}");
    }

    private static GameState CreateState(int attackerTurnsTaken, int allyHp, int enemyHp)
    {
        var attacker = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(0, 0), hp: allyHp);
        var defender = EngineTestFactory.CreateUnit(2, team: 2, position: new HexCoord(1, 0), hp: enemyHp);

        return EngineTestFactory.CreateState(
            new[] { attacker, defender },
            teamToAct: 1,
            activeUnitId: attacker.Id,
            attackerTurnsTaken: attackerTurnsTaken);
    }
}
