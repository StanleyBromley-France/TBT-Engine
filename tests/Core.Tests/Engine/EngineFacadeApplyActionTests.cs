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

namespace Core.Tests.Engine;

public class EngineFacadeApplyActionTests
{
    [Fact]
    public void ApplyAction_Throws_When_Action_Is_Null()
    {
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var state = EngineTestFactory.CreateState(new[] { unit }, teamToAct: 1, activeUnitId: unit.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var facade = CreateFacade(session, isActionLegal: true, dispatcher: (_, _, _) => { }, gameOverResult: GameOutcome.Ongoing());

        Assert.Throws<ArgumentNullException>(() => facade.ApplyAction(null!));
    }

    [Fact]
    public void ApplyAction_Throws_For_Illegal_Action_Without_Dispatch_Or_Undo_Commit()
    {
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var state = EngineTestFactory.CreateState(new[] { unit }, teamToAct: 1, activeUnitId: unit.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var dispatcherSpy = new DispatcherSpy();
        var gameOverSpy = new FixedGameOverEvaluator(GameOutcome.Ongoing());
        var effectSpy = new EffectManagerSpy();
        var facade = new EngineFacade(
            session,
            new StubActionRules(new FixedActionValidator(false), new EmptyActionGenerator()),
            dispatcherSpy,
            new DeterministicRng(),
            effectSpy,
            gameOverSpy);

        Assert.Throws<InvalidOperationException>(() => facade.ApplyAction(new SkipActiveUnitAction(unit.Id)));
        Assert.Equal(0, dispatcherSpy.CallCount);
        Assert.Empty(session.Undo.Records);
        Assert.Equal(0, gameOverSpy.CallCount);
        Assert.Equal(0, effectSpy.TickAllCount);
    }

    [Fact]
    public void ApplyAction_Dispatches_And_Commits_Undo_Without_Turn_Advance_When_Units_Remain_Uncommitted()
    {
        // Arrange: active team still has another uncommitted ally
        var active = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var allyUncommitted = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0));
        var enemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(2, 0));
        var state = EngineTestFactory.CreateState(
            new[] { active, allyUncommitted, enemy },
            teamToAct: 1,
            activeUnitId: active.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var dispatcherSpy = new DispatcherSpy((s, ctx, action) => ctx.Turn.CommitUnit(action.UnitId));
        var gameOverSpy = new FixedGameOverEvaluator(GameOutcome.Ongoing());
        var effectSpy = new EffectManagerSpy();
        var facade = new EngineFacade(
            session,
            new StubActionRules(new FixedActionValidator(true), new EmptyActionGenerator()),
            dispatcherSpy,
            new DeterministicRng(),
            effectSpy,
            gameOverSpy);

        // Act: dispatch commits only the acting unit
        facade.ApplyAction(new SkipActiveUnitAction(active.Id));

        // Assert: operation committed, but no turn advance/start-of-turn resolution happened
        Assert.Equal(1, dispatcherSpy.CallCount);
        Assert.Single(session.Undo.Records);
        Assert.True(session.State.Phase.HasCommitted(active.Id));
        Assert.Equal(new TeamId(1), session.State.Turn.TeamToAct);
        Assert.Equal(0, effectSpy.TickAllCount);
        Assert.Equal(1, gameOverSpy.CallCount);
        Assert.Equal(GameOutcomeType.Ongoing, session.Outcome.Type);
    }

    [Fact]
    public void ApplyAction_Advances_Turn_Resets_Next_Team_And_Updates_Outcome()
    {
        // Arrange: only one attacker is alive, so committing it should end team turn
        var attacker = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var defender = EngineTestFactory.CreateUnit(2, 2, new HexCoord(3, 0));
        defender.Resources.ActionPoints = 0;
        defender.Resources.MovePoints = 0;

        var state = EngineTestFactory.CreateState(
            new[] { attacker, defender },
            teamToAct: 1,
            activeUnitId: attacker.Id,
            attackerTurnsTaken: 2);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var dispatcherSpy = new DispatcherSpy((s, ctx, action) => ctx.Turn.CommitUnit(action.UnitId));
        var winningOutcome = GameOutcome.Victory(new TeamId(2));
        var gameOverSpy = new FixedGameOverEvaluator(winningOutcome);
        var effectSpy = new EffectManagerSpy();
        var facade = new EngineFacade(
            session,
            new StubActionRules(new FixedActionValidator(true), new EmptyActionGenerator()),
            dispatcherSpy,
            new DeterministicRng(),
            effectSpy,
            gameOverSpy);

        // Act: commit attacker, triggering turn advance and start-of-turn reset for defender
        facade.ApplyAction(new SkipActiveUnitAction(attacker.Id));

        // Assert: turn switched, defender reset, effect tick executed, and outcome applied
        Assert.Equal(1, dispatcherSpy.CallCount);
        Assert.Equal(new TeamId(2), session.State.Turn.TeamToAct);
        Assert.Equal(3, session.State.Turn.AttackerTurnsTaken);
        Assert.Equal(defender.Id, session.State.Phase.ActiveUnitId);
        Assert.Equal(defender.DerivedStats.MaxActionPoints, defender.Resources.ActionPoints);
        Assert.Equal(defender.DerivedStats.MaxMovePoints, defender.Resources.MovePoints);
        Assert.Equal(1, effectSpy.TickAllCount);
        Assert.Equal(GameOutcomeType.Victory, session.Outcome.Type);
        Assert.Equal(new TeamId(2), session.Outcome.WinningTeam);
    }

    private static EngineFacade CreateFacade(
        GameSession session,
        bool isActionLegal,
        Action<IReadOnlyGameState, GameMutationContext, ActionChoice> dispatcher,
        GameOutcome gameOverResult)
    {
        return new EngineFacade(
            session,
            new StubActionRules(new FixedActionValidator(isActionLegal), new EmptyActionGenerator()),
            new DispatcherSpy(dispatcher),
            new DeterministicRng(),
            new EffectManagerSpy(),
            new FixedGameOverEvaluator(gameOverResult));
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
        public int CallCount { get; private set; }

        public DispatcherSpy(Action<IReadOnlyGameState, GameMutationContext, ActionChoice>? onExecute = null)
        {
            _onExecute = onExecute ?? ((_, _, _) => { });
        }

        public void Execute(IReadOnlyGameState state, GameMutationContext ctx, ActionChoice action)
        {
            CallCount++;
            _onExecute(state, ctx, action);
        }
    }

    private sealed class EffectManagerSpy : IEffectManager
    {
        public int TickAllCount { get; private set; }

        public void ApplyOrStackEffect(GameMutationContext context, IReadOnlyGameState state, EffectApplicationRequest request)
        {
        }

        public void TickAll(GameMutationContext context, IReadOnlyGameState state)
        {
            TickAllCount++;
        }
    }

    private sealed class FixedGameOverEvaluator : IGameOverEvaluator
    {
        private readonly GameOutcome _outcome;
        public int CallCount { get; private set; }

        public FixedGameOverEvaluator(GameOutcome outcome)
        {
            _outcome = outcome;
        }

        public GameOutcome Evaluate(GameSession session)
        {
            CallCount++;
            return _outcome;
        }
    }
}
