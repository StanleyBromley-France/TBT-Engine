using Core.Domain.Abilities;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Effects;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Engine.Rules;
using Core.Engine.Victory;
using Core.Game;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Undo;

public class EngineFacadeUndoTests
{
    [Fact]
    public void UndoLastAction_Rewinds_State_And_Recomputes_Outcome()
    {
        var active = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10);
        var allyUncommitted = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0));
        var enemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(2, 0));
        var state = EngineTestFactory.CreateState(
            new[] { active, allyUncommitted, enemy },
            teamToAct: 1,
            activeUnitId: active.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var dispatcher = new DispatcherSpy((s, ctx, action) =>
        {
            ctx.Units.ChangeMana(action.UnitId, -1);
            ctx.Turn.CommitUnit(action.UnitId);
        });
        var gameOver = new ManaThresholdGameOverEvaluator(active.Id, thresholdInclusive: 9, winningTeam: new TeamId(1));
        var facade = new EngineFacade(
            session,
            new StubActionRules(new FixedActionValidator(true), new EmptyActionGenerator()),
            dispatcher,
            new DeterministicRng(),
            new EffectManagerSpy(),
            gameOver);

        facade.ApplyAction(new SkipActiveUnitAction(active.Id));
        Assert.Equal(9, active.Resources.Mana);
        Assert.True(state.Phase.HasCommitted(active.Id));
        Assert.Equal(GameOutcomeType.Victory, session.Outcome.Type);

        facade.UndoLastAction();

        Assert.Equal(10, active.Resources.Mana);
        Assert.False(state.Phase.HasCommitted(active.Id));
        Assert.Empty(session.Undo.Records);
        Assert.Equal(GameOutcomeType.Ongoing, session.Outcome.Type);
    }

    [Fact]
    public void UndoTo_Rewinds_All_Actions_Back_To_Marker_And_Recomputes_Outcome()
    {
        var active = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10);
        var allyUncommitted = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0));
        var enemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(2, 0));
        var state = EngineTestFactory.CreateState(
            new[] { active, allyUncommitted, enemy },
            teamToAct: 1,
            activeUnitId: active.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var dispatcher = new DispatcherSpy((s, ctx, action) =>
        {
            ctx.Units.ChangeMana(action.UnitId, -1);
            ctx.Turn.CommitUnit(action.UnitId);
        });
        var gameOver = new ManaThresholdGameOverEvaluator(active.Id, thresholdInclusive: 9, winningTeam: new TeamId(1));
        var facade = new EngineFacade(
            session,
            new StubActionRules(new FixedActionValidator(true), new EmptyActionGenerator()),
            dispatcher,
            new DeterministicRng(),
            new EffectManagerSpy(),
            gameOver);

        var marker = facade.MarkUndo();
        facade.ApplyAction(new SkipActiveUnitAction(active.Id));
        facade.ApplyAction(new SkipActiveUnitAction(active.Id));
        Assert.Equal(8, active.Resources.Mana);
        Assert.Equal(2, session.Undo.Records.Count);
        Assert.Equal(GameOutcomeType.Victory, session.Outcome.Type);

        facade.UndoTo(marker);

        Assert.Equal(10, active.Resources.Mana);
        Assert.False(state.Phase.HasCommitted(active.Id));
        Assert.Empty(session.Undo.Records);
        Assert.Equal(GameOutcomeType.Ongoing, session.Outcome.Type);
    }

    [Fact]
    public void UndoLastAction_After_Mixed_Action_Sequence_Restores_Initial_State()
    {
        var unitA = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10);
        var unitB = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0), mana: 8);
        var enemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(3, 0), mana: 7);
        var state = EngineTestFactory.CreateState(
            new[] { unitA, unitB, enemy },
            teamToAct: 1,
            activeUnitId: unitA.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var facade = new EngineFacade(
            session,
            new StubActionRules(new FixedActionValidator(true), new EmptyActionGenerator()),
            new DispatcherSpy((s, ctx, action) =>
            {
                switch (action)
                {
                    case ChangeActiveUnitAction change:
                        ctx.Turn.ChangeActiveUnit(change.NewActiveUnitId);
                        break;
                    case MoveAction move:
                        ctx.Movement.MoveUnit(move.UnitId, move.TargetHex);
                        ctx.Units.ChangeMovePoints(move.UnitId, -1);
                        ctx.Units.ChangeActionPoints(move.UnitId, -1);
                        ctx.Turn.CommitUnit(move.UnitId);
                        break;
                    case UseAbilityAction use:
                        ctx.Units.ChangeMana(use.UnitId, -2);
                        ctx.Units.ChangeActionPoints(use.UnitId, -1);
                        ctx.Turn.CommitUnit(use.UnitId);
                        break;
                    case SkipActiveUnitAction skip:
                        var ap = s.UnitInstances[skip.UnitId].Resources.ActionPoints;
                        if (ap != 0)
                            ctx.Units.ChangeActionPoints(skip.UnitId, -ap);
                        ctx.Turn.CommitUnit(skip.UnitId);
                        break;
                }
            }),
            new DeterministicRng(),
            new EffectManagerSpy(),
            new FixedGameOverEvaluator(GameOutcome.Ongoing()));

        var initial = Snapshot.From(session);

        facade.ApplyAction(new ChangeActiveUnitAction(unitA.Id, unitB.Id));
        facade.ApplyAction(new MoveAction(unitB.Id, new HexCoord(2, 0)));
        facade.ApplyAction(new UseAbilityAction(unitB.Id, new AbilityId("test-ability"), enemy.Id));
        facade.ApplyAction(new SkipActiveUnitAction(unitB.Id));
        facade.ApplyAction(new ChangeActiveUnitAction(unitB.Id, unitA.Id));
        facade.ApplyAction(new MoveAction(unitA.Id, new HexCoord(0, 1)));
        facade.ApplyAction(new SkipActiveUnitAction(unitA.Id));

        while (session.Undo.CanUndo)
            facade.UndoLastAction();

        var final = Snapshot.From(session);
        Assert.Equal(initial.TeamToAct, final.TeamToAct);
        Assert.Equal(initial.AttackerTurnsTaken, final.AttackerTurnsTaken);
        Assert.Equal(initial.ActiveUnitId, final.ActiveUnitId);
        Assert.Equal(initial.CommittedUnits, final.CommittedUnits);
        Assert.Equal(initial.Units, final.Units);
        Assert.Equal(initial.OccupiedHexes, final.OccupiedHexes);
        Assert.Equal(GameOutcomeType.Ongoing, session.Outcome.Type);
    }

    private sealed class StubActionRules : IActionRules
    {
        public IActionValidator Validator { get; }
        public IActionGenerator Generator { get; }

        public StubActionRules(IActionValidator validator, IActionGenerator generator)
        {
            Validator = validator;
            Generator = generator;
        }
    }

    private sealed class FixedActionValidator : IActionValidator
    {
        private readonly bool _result;

        public FixedActionValidator(bool result)
        {
            _result = result;
        }

        public bool IsActionLegal(IReadOnlyGameState state, ActionChoice action) => _result;
    }

    private sealed class EmptyActionGenerator : IActionGenerator
    {
        public IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state) => Array.Empty<ActionChoice>();
    }

    private sealed class DispatcherSpy : IActionDispatcher
    {
        private readonly Action<IReadOnlyGameState, GameMutationContext, ActionChoice> _onExecute;

        public DispatcherSpy(Action<IReadOnlyGameState, GameMutationContext, ActionChoice>? onExecute = null)
        {
            _onExecute = onExecute ?? ((_, _, _) => { });
        }

        public void Execute(IReadOnlyGameState state, GameMutationContext ctx, ActionChoice action)
        {
            _onExecute(state, ctx, action);
        }
    }

    private sealed class EffectManagerSpy : IEffectManager
    {
        public void ApplyOrStackEffect(GameMutationContext context, IReadOnlyGameState state, EffectApplicationRequest request)
        {
        }

        public void TickAll(GameMutationContext context, IReadOnlyGameState state)
        {
        }
    }

    private sealed class FixedGameOverEvaluator : IGameOverEvaluator
    {
        private readonly GameOutcome _outcome;

        public FixedGameOverEvaluator(GameOutcome outcome)
        {
            _outcome = outcome;
        }

        public GameOutcome Evaluate(GameSession session) => _outcome;
    }

    private sealed class ManaThresholdGameOverEvaluator : IGameOverEvaluator
    {
        private readonly UnitInstanceId _unitId;
        private readonly int _thresholdInclusive;
        private readonly TeamId _winningTeam;

        public ManaThresholdGameOverEvaluator(UnitInstanceId unitId, int thresholdInclusive, TeamId winningTeam)
        {
            _unitId = unitId;
            _thresholdInclusive = thresholdInclusive;
            _winningTeam = winningTeam;
        }

        public GameOutcome Evaluate(GameSession session)
        {
            var mana = session.State.UnitInstances[_unitId].Resources.Mana;
            return mana <= _thresholdInclusive ? GameOutcome.Victory(_winningTeam) : GameOutcome.Ongoing();
        }
    }

    private sealed record Snapshot(
        TeamId TeamToAct,
        int AttackerTurnsTaken,
        UnitInstanceId ActiveUnitId,
        UnitInstanceId[] CommittedUnits,
        UnitSnapshot[] Units,
        HexCoord[] OccupiedHexes)
    {
        public static Snapshot From(GameSession session)
        {
            var state = session.State;
            var units = state.UnitInstances.Values
                .OrderBy(u => u.Id.Value)
                .Select(u => new UnitSnapshot(
                    u.Id,
                    u.Position,
                    u.Resources.HP,
                    u.Resources.Mana,
                    u.Resources.ActionPoints,
                    u.Resources.MovePoints))
                .ToArray();

            return new Snapshot(
                state.Turn.TeamToAct,
                state.Turn.AttackerTurnsTaken,
                state.Phase.ActiveUnitId,
                state.Phase.CommittedThisPhase.OrderBy(id => id.Value).ToArray(),
                units,
                state.OccupiedHexes.OrderBy(h => h.Q).ThenBy(h => h.R).ToArray());
        }
    }

    private sealed record UnitSnapshot(
        UnitInstanceId Id,
        HexCoord Position,
        int HP,
        int Mana,
        int ActionPoints,
        int MovePoints);
}
