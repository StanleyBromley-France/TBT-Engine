using Agents.Mcts.Config;
using Agents.Mcts.Evaluation;
using Agents.Mcts.Policies;
using Agents.Mcts.Search;
using Agents.Mcts.Simulation;
using Core.Domain.Abilities;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine;
using Core.Engine.Actions.Choice;
using Core.Engine.Turn;
using Core.Game.Match;
using Core.Game.Session;
using Core.Game.State.ReadOnly;
using Core.Tests.Engine.TestSupport;
using Xunit.Abstractions;

namespace Core.Tests.Agents.Mcts;

public sealed class MctsAgentIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public MctsAgentIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void MctsVsMcts_SmokeRun_Completes_Without_Illegal_Actions()
    {
        var scenario = CreateScenario();

        var result = RunMatch(
            scenario,
            maxTurnLimit: scenario.MaxTurnLimit,
            maxActionBudget: 200);

        Assert.Equal(0, result.IllegalActionSelections);
        Assert.True(result.CompletedWithoutException);
        Assert.True(result.StepCount > 0, "Expected at least one applied action.");
        Assert.True(
            result.ReachedTerminalState || result.HitTurnLimit,
            $"Expected terminal state or turn limit. StepCount={result.StepCount}, AttackerTurns={scenario.Engine.GetState().Turn.AttackerTurnsTaken}");
        Assert.True(result.StateValidityChecks > 0, "Expected state validity checks to run.");
    }

    [Fact]
    public void ChooseAction_Uses_Sandbox_Without_Mutating_Live_Engine()
    {
        var scenario = CreateScenario();
        var context = new TurnPolicyContext(scenario.Engine);
        var state = scenario.Engine.GetState();
        var legalActions = scenario.Engine.GetLegalActions().ToList();

        var snapshotBefore = CaptureSnapshot(scenario.Session);
        var undoBefore = scenario.Session.Runtime.Undo.Records.Count;

        var chosen = scenario.AttackerPolicy.ChooseAction(context, state, legalActions);

        var snapshotAfter = CaptureSnapshot(scenario.Session);
        var undoAfter = scenario.Session.Runtime.Undo.Records.Count;

        Assert.Equal(snapshotBefore, snapshotAfter);
        Assert.Equal(undoBefore, undoAfter);
        Assert.Contains(legalActions, candidate => ActionsEquivalent(candidate, chosen));
    }

    [Fact]
    public void ChooseAction_Resulting_State_Matches_Between_Sandbox_And_Live_Engine()
    {
        var scenario = CreateScenario();
        var context = new TurnPolicyContext(scenario.Engine);
        var state = scenario.Engine.GetState();
        var legalActions = scenario.Engine.GetLegalActions().ToList();

        Assert.NotEmpty(legalActions);

        var chosen = scenario.AttackerPolicy.ChooseAction(context, state, legalActions);
        Assert.Contains(legalActions, candidate => ActionsEquivalent(candidate, chosen));

        // Reproduce the chosen action from the same pre-state on an independent sandbox engine.
        var sandboxEngine = scenario.Engine.CreateSandbox();
        sandboxEngine.ApplyAction(chosen);
        scenario.Engine.ApplyAction(chosen);

        var sandboxFingerprint = CaptureStateFingerprint(sandboxEngine.GetState());
        var liveFingerprint = CaptureStateFingerprint(scenario.Engine.GetState());

        Assert.Equal(sandboxFingerprint, liveFingerprint);

        var sandboxLegal = sandboxEngine.GetLegalActions().Select(FormatAction).OrderBy(x => x).ToArray();
        var liveLegal = scenario.Engine.GetLegalActions().Select(FormatAction).OrderBy(x => x).ToArray();
        Assert.Equal(sandboxLegal, liveLegal);
    }

    [Fact]
    public void MctsSearch_FindBestAction_Does_Not_Mutate_Simulation_State_After_Exploration()
    {
        var scenario = CreateScenario();
        var sandboxFactory = new EngineSandboxFactory();
        var simulation = sandboxFactory.CreateFrom(scenario.Engine);
        var search = new MctsSearch(new MaterialStateEvaluator());
        var config = new MctsSearchConfig
        {
            IterationBudget = 64,
            MaxDepth = 4,
            ExplorationConstant = 1.41
        };

        var baselineStateFingerprint = CaptureStateFingerprint(simulation.GetState());
        var baselineLegal = simulation.GetLegalActions().ToList();
        var baselineLegalSignatures = baselineLegal.Select(FormatAction).OrderBy(x => x).ToArray();

        Assert.NotEmpty(baselineLegal);

        for (var i = 0; i < 10; i++)
        {
            var selected = search.FindBestAction(simulation, config);
            Assert.Contains(baselineLegal, candidate => ActionsEquivalent(candidate, selected));

            var afterStateFingerprint = CaptureStateFingerprint(simulation.GetState());
            var afterLegalSignatures = simulation.GetLegalActions().Select(FormatAction).OrderBy(x => x).ToArray();

            Assert.Equal(baselineStateFingerprint, afterStateFingerprint);
            Assert.Equal(baselineLegalSignatures, afterLegalSignatures);
        }
    }

    private Scenario CreateScenario()
    {
        const int maxTurnLimit = 12;
        var attackerTeam = new TeamId(1);
        var defenderTeam = new TeamId(2);

        // Minimal deterministic 2v2 setup; no ability data needed for current contract tests.
        var attackerOne = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(1, 1));
        var attackerTwo = EngineTestFactory.CreateUnit(2, team: 1, position: new HexCoord(1, 3));
        var defenderOne = EngineTestFactory.CreateUnit(3, team: 2, position: new HexCoord(5, 1));
        var defenderTwo = EngineTestFactory.CreateUnit(4, team: 2, position: new HexCoord(5, 3));

        var state = EngineTestFactory.CreateState(
            new[] { attackerOne, attackerTwo, defenderOne, defenderTwo },
            teamToAct: attackerTeam.Value,
            activeUnitId: attackerOne.Id);

        var session = EngineTestFactory.CreateSession(
            state,
            new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var engine = EngineCompositionRoot.Create(session, turnCount: maxTurnLimit);

        var evaluator = new MaterialStateEvaluator();
        var search = new MctsSearch(evaluator);
        var sandboxFactory = new EngineSandboxFactory();
        var config = new MctsSearchConfig
        {
            IterationBudget = 64,
            MaxDepth = 4,
            ExplorationConstant = 1.41
        };

        var attackerPolicy = new MctsTurnPolicy(search, sandboxFactory, config);
        var defenderPolicy = new MctsTurnPolicy(search, sandboxFactory, config);

        return new Scenario(
            Engine: engine,
            Session: session,
            AttackerPolicy: attackerPolicy,
            DefenderPolicy: defenderPolicy,
            AttackerTeam: attackerTeam,
            DefenderTeam: defenderTeam,
            MaxTurnLimit: maxTurnLimit);
    }

    private MatchRunResult RunMatch(Scenario scenario, int maxTurnLimit, int maxActionBudget)
    {
        var engine = scenario.Engine;
        var session = scenario.Session;
        var context = new TurnPolicyContext(engine);

        var stepCount = 0;
        var illegalActionSelections = 0;
        var stateValidityChecks = 0;

        while (!engine.IsGameOver() &&
               engine.GetState().Turn.AttackerTurnsTaken <= maxTurnLimit &&
               stepCount < maxActionBudget)
        {
            stepCount++;
            var state = engine.GetState();
            AssertStateIsValid(state);
            stateValidityChecks++;

            var legalActions = engine.GetLegalActions().ToList();
            Assert.NotEmpty(legalActions);

            var policy = state.Turn.TeamToAct == scenario.AttackerTeam
                ? scenario.AttackerPolicy
                : scenario.DefenderPolicy;

            var snapshotBeforeChoose = CaptureSnapshot(session);
            var undoBeforeChoose = session.Runtime.Undo.Records.Count;

            var chosenAction = policy.ChooseAction(context, state, legalActions);

            var snapshotAfterChoose = CaptureSnapshot(session);
            Assert.Equal(snapshotBeforeChoose, snapshotAfterChoose);
            Assert.Equal(undoBeforeChoose, session.Runtime.Undo.Records.Count);

            var legalSelection = legalActions.Any(candidate => ActionsEquivalent(candidate, chosenAction));
            if (!legalSelection)
                illegalActionSelections++;

            Assert.True(
                legalSelection,
                $"Policy selected an illegal action: {FormatAction(chosenAction)}");

            var hpBefore = SumAliveHp(state);
            LogBeforeAction(
                step: stepCount,
                state: state,
                legalActionCount: legalActions.Count,
                chosenAction: chosenAction,
                attackerTeam: scenario.AttackerTeam,
                defenderTeam: scenario.DefenderTeam);

            engine.ApplyAction(chosenAction);

            Assert.Equal(undoBeforeChoose + 1, session.Runtime.Undo.Records.Count);

            var stateAfter = engine.GetState();
            AssertStateIsValid(stateAfter);
            stateValidityChecks++;

            var hpAfter = SumAliveHp(stateAfter);

            LogAfterAction(
                step: stepCount,
                state: stateAfter,
                chosenAction: chosenAction,
                hpBefore: hpBefore,
                hpAfter: hpAfter,
                attackerTeam: scenario.AttackerTeam,
                defenderTeam: scenario.DefenderTeam);

        }

        var reachedTerminalState = engine.IsGameOver();
        var exhaustedBudget = stepCount >= maxActionBudget;
        // Treat budget exhaustion as a safety turn-limit guard for non-terminating loops.
        var hitTurnLimit = engine.GetState().Turn.AttackerTurnsTaken > maxTurnLimit || exhaustedBudget;

        return new MatchRunResult(
            CompletedWithoutException: true,
            ReachedTerminalState: reachedTerminalState,
            HitTurnLimit: hitTurnLimit,
            StepCount: stepCount,
            IllegalActionSelections: illegalActionSelections,
            StateValidityChecks: stateValidityChecks);
    }

    private void LogProgress(string message)
    {
        _output.WriteLine(message);
        Console.WriteLine(message);
    }

    private void LogBeforeAction(
        int step,
        IReadOnlyGameState state,
        int legalActionCount,
        ActionChoice chosenAction,
        TeamId attackerTeam,
        TeamId defenderTeam)
    {
        LogProgress(
            $"[BEFORE] step={step} team={state.Turn.TeamToAct} attackerTurns={state.Turn.AttackerTurnsTaken} " +
            $"active={state.Phase.ActiveUnitId} committed=[{FormatCommitted(state)}] legal={legalActionCount} action={FormatAction(chosenAction)} " +
            $"units={BuildResourceSummary(state, attackerTeam, defenderTeam)}");
    }

    private void LogAfterAction(
        int step,
        IReadOnlyGameState state,
        ActionChoice chosenAction,
        int hpBefore,
        int hpAfter,
        TeamId attackerTeam,
        TeamId defenderTeam)
    {
        LogProgress(
            $"[AFTER ] step={step} team={state.Turn.TeamToAct} attackerTurns={state.Turn.AttackerTurnsTaken} " +
            $"active={state.Phase.ActiveUnitId} committed=[{FormatCommitted(state)}] action={FormatAction(chosenAction)} " +
            $"hpDelta={hpAfter - hpBefore} units={BuildResourceSummary(state, attackerTeam, defenderTeam)}");
    }

    private static void AssertStateIsValid(IReadOnlyGameState state)
    {
        Assert.True(state.UnitInstances.ContainsKey(state.Phase.ActiveUnitId));
        var active = state.UnitInstances[state.Phase.ActiveUnitId];
        Assert.True(active.IsAlive);
        Assert.Equal(state.Turn.TeamToAct, active.Team);

        var alivePositions = new HashSet<HexCoord>();

        foreach (var unit in state.UnitInstances.Values)
        {
            Assert.True(
                state.Map.TryGetTile(unit.Position, out var tile),
                $"Unit {unit.Id} is placed outside the map at {unit.Position}.");

            if (unit.IsAlive)
            {
                Assert.True(tile.IsWalkable, $"Unit {unit.Id} is on non-walkable terrain at {unit.Position}.");
                alivePositions.Add(unit.Position);
            }
        }

        Assert.True(alivePositions.SetEquals(state.OccupiedHexes));

        foreach (var committed in state.Phase.CommittedThisPhase)
            Assert.True(state.UnitInstances.ContainsKey(committed));
    }

    private static int SumAliveHp(IReadOnlyGameState state)
    {
        return state.UnitInstances.Values
            .Where(unit => unit.IsAlive)
            .Sum(unit => unit.Resources.HP);
    }

    private static string BuildResourceSummary(IReadOnlyGameState state, TeamId attackerTeam, TeamId defenderTeam)
    {
        static string FormatTeam(IReadOnlyGameState state, TeamId team)
        {
            var members = state.UnitInstances.Values
                .Where(unit => unit.Team == team)
                .OrderBy(unit => unit.Id.Value)
                .Select(unit => $"{unit.Id}@{unit.Position}:HP={unit.Resources.HP},AP={unit.Resources.ActionPoints},MP={unit.Resources.MovePoints},M={unit.Resources.Mana}")
                .ToArray();

            return string.Join(",", members);
        }

        return $"T{attackerTeam}=[{FormatTeam(state, attackerTeam)}] T{defenderTeam}=[{FormatTeam(state, defenderTeam)}]";
    }

    private static string FormatCommitted(IReadOnlyGameState state)
    {
        return string.Join(
            ",",
            state.Phase.CommittedThisPhase
                .OrderBy(id => id.Value)
                .Select(id => id.Value));
    }

    private static string FormatAction(ActionChoice action)
    {
        return action switch
        {
            MoveAction move => $"Move(unit={move.UnitId},to={move.TargetHex})",
            UseAbilityAction use => $"UseAbility(unit={use.UnitId},ability={use.AbilityId},target={use.Target})",
            ChangeActiveUnitAction change => $"ChangeActive(unit={change.UnitId},to={change.NewActiveUnitId})",
            SkipActiveUnitAction skip => $"Skip(unit={skip.UnitId})",
            _ => action.GetType().Name
        };
    }

    private static bool ActionsEquivalent(ActionChoice left, ActionChoice right)
    {
        if (left.GetType() != right.GetType())
            return false;

        return left switch
        {
            MoveAction l when right is MoveAction r =>
                l.UnitId == r.UnitId &&
                l.TargetHex == r.TargetHex,

            UseAbilityAction l when right is UseAbilityAction r =>
                l.UnitId == r.UnitId &&
                l.AbilityId == r.AbilityId &&
                l.Target == r.Target,

            ChangeActiveUnitAction l when right is ChangeActiveUnitAction r =>
                l.UnitId == r.UnitId &&
                l.NewActiveUnitId == r.NewActiveUnitId,

            SkipActiveUnitAction l when right is SkipActiveUnitAction r =>
                l.UnitId == r.UnitId,

            _ => false
        };
    }

    private static EngineSnapshot CaptureSnapshot(GameSession session)
    {
        var state = session.Runtime.State;
        var units = state.UnitInstances.Values
            .OrderBy(unit => unit.Id.Value)
            .Select(unit => $"{unit.Id}|{unit.Team}|{unit.Position}|{unit.Resources.HP}|{unit.Resources.Mana}|{unit.Resources.ActionPoints}|{unit.Resources.MovePoints}")
            .ToArray();

        var committed = state.Phase.CommittedThisPhase
            .OrderBy(id => id.Value)
            .Select(id => id.Value)
            .ToArray();

        var occupied = state.OccupiedHexes
            .OrderBy(hex => hex.Q)
            .ThenBy(hex => hex.R)
            .Select(hex => $"{hex.Q}:{hex.R}")
            .ToArray();

        return new EngineSnapshot(
            TeamToAct: state.Turn.TeamToAct,
            AttackerTurnsTaken: state.Turn.AttackerTurnsTaken,
            ActiveUnitId: state.Phase.ActiveUnitId,
            Committed: string.Join(",", committed),
            Units: string.Join(";", units),
            Occupied: string.Join(";", occupied),
            UndoCount: session.Runtime.Undo.Records.Count,
            OutcomeType: session.Runtime.Outcome.Type,
            WinningTeam: session.Runtime.Outcome.WinningTeam);
    }

    private static string CaptureStateFingerprint(IReadOnlyGameState state)
    {
        var units = state.UnitInstances.Values
            .OrderBy(unit => unit.Id.Value)
            .Select(unit => $"{unit.Id}|{unit.Team}|{unit.Position}|{unit.Resources.HP}|{unit.Resources.Mana}|{unit.Resources.ActionPoints}|{unit.Resources.MovePoints}")
            .ToArray();

        var committed = state.Phase.CommittedThisPhase
            .OrderBy(id => id.Value)
            .Select(id => id.Value)
            .ToArray();

        var occupied = state.OccupiedHexes
            .OrderBy(hex => hex.Q)
            .ThenBy(hex => hex.R)
            .Select(hex => $"{hex.Q}:{hex.R}")
            .ToArray();

        return string.Join(
            "|",
            state.Turn.TeamToAct,
            state.Turn.AttackerTurnsTaken,
            state.Phase.ActiveUnitId,
            string.Join(",", committed),
            string.Join(";", units),
            string.Join(";", occupied),
            state.Rng.Seed,
            state.Rng.Position);
    }

    private sealed record Scenario(
        EngineFacade Engine,
        GameSession Session,
        MctsTurnPolicy AttackerPolicy,
        MctsTurnPolicy DefenderPolicy,
        TeamId AttackerTeam,
        TeamId DefenderTeam,
        int MaxTurnLimit);

    private sealed record EngineSnapshot(
        TeamId TeamToAct,
        int AttackerTurnsTaken,
        UnitInstanceId ActiveUnitId,
        string Committed,
        string Units,
        string Occupied,
        int UndoCount,
        GameOutcomeType OutcomeType,
        TeamId? WinningTeam);

    private sealed record MatchRunResult(
        bool CompletedWithoutException,
        bool ReachedTerminalState,
        bool HitTurnLimit,
        int StepCount,
        int IllegalActionSelections,
        int StateValidityChecks);
}
