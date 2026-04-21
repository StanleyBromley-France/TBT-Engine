namespace Core.Engine;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Effects;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Engine.Rules;
using Core.Engine.Telemetry;
using Core.Engine.Victory;
using Core.Game.Match;
using Core.Game.Session;
using Core.Game.State.ReadOnly;
using Core.Undo;

/// <summary>
/// Top-level orchestrator and entry point into the simulation.
///
/// Responsibilities:
/// - Defines operation boundaries (action, tick, turn advance)
/// - Creates a GameMutationContext per operation
/// - Routes mutations through rules and effect manager
/// - Exposes read-only queries (state, legal actions, game over)
///
/// All game state mutation flows through GameMutationContext.
/// </summary>
public sealed class EngineFacade
{
    private readonly GameSession _session;
    private readonly IActionRules _rules;
    private readonly IActionDispatcher _dispatcher;
    private readonly DeterministicRng _rngService;
    private readonly IEffectManager _effectManager;
    private readonly IGameOverEvaluator _gameOver;
    private readonly ICombatTelemetrySink _combatTelemetry;
    internal EngineFacade(
        GameSession session,
        IActionRules rules,
        IActionDispatcher dispatcher,
        DeterministicRng rngService,
        IEffectManager effectManager,
        IGameOverEvaluator gameOver,
        ICombatTelemetrySink? combatTelemetry = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _rngService = rngService ?? throw new ArgumentNullException(nameof(rngService));
        _effectManager = effectManager ?? throw new ArgumentNullException(nameof(effectManager));
        _gameOver = gameOver ?? throw new ArgumentNullException(nameof(_gameOver));
        _combatTelemetry = combatTelemetry ?? NullCombatTelemetrySink.Instance;
    }

    public TemplateRegistry GetContent() => _session.Context.Content;

    public IReadOnlyGameState GetState() => _session.Runtime.State;

    public GameOutcome GetOutcome() => _session.Runtime.Outcome;

    public IEnumerable<ActionChoice> GetLegalActions()
        => _rules.Generator.GetLegalActions(_session.Runtime.State);

    public UndoMarker MarkUndo() => _session.Runtime.Undo.Mark();

    /// <summary>
    /// Applies exactly one player decision (ActionChoice).
    /// This method is the primary undo boundary: one UndoRecord per call.
    /// Any automatic resolution (end/start of turn, effect ticks, team swap)
    /// is performed inside the same operation and committed once.
    /// </summary>
    public void ApplyAction(ActionChoice action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var state = _session.Runtime.State;

        if (!_rules.Validator.IsActionLegal(state, action))
            throw new InvalidOperationException("Illegal action.");

        var undo = new UndoRecord();
        var ctx = CreateContext(undo);

        _dispatcher.Execute(state, ctx, action);

        ResolvePostAction(ctx, state);

        Commit(undo);

        // Keep outcome evaluation at operation boundary for non-advance paths.
        TrySetGameOver();
    }

    public void UndoLastAction()
    {
        _session.Runtime.Undo.UndoLast(_session.Runtime.State);
        RecomputeOutcome();
    }

    public void UndoTo(UndoMarker marker)
    {
        _session.Runtime.Undo.UndoTo(_session.Runtime.State, marker);
        RecomputeOutcome();
    }

    public bool IsGameOver()
    {
        return _session.Runtime.Outcome.Type == GameOutcomeType.Victory || _session.Runtime.Outcome.Type == GameOutcomeType.Draw;
    }

    public EngineFacade CreateSandbox()
    {
        var sandboxSession = _session.CreateSandbox();

        return new EngineFacade(
            session: sandboxSession,
            rules: _rules,
            dispatcher: _dispatcher,
            rngService: _rngService,
            effectManager: _effectManager,
            gameOver: _gameOver,
            combatTelemetry: NullCombatTelemetrySink.Instance);
    }

    // Post apply action resolution

    private void ResolvePostAction(GameMutationContext ctx, IReadOnlyGameState state)
    {
        if (!ShouldAdvanceTurn(state))
            return;

        var currentTeam = state.Turn.TeamToAct;
        var nextTeam = _session.Context.Teams.GetOpposingTeam(currentTeam);

        // If the next team has no living units, end the game without switching turn ownership.
        if (!TeamHasLivingUnits(state, nextTeam))
        {
            TrySetGameOver();
            return;
        }

        AdvanceTurn(ctx, state);
        ResolveStartOfTurn(ctx, state);
    }

    private static bool ShouldAdvanceTurn(IReadOnlyGameState state)
    {
        var team = state.Turn.TeamToAct;

        foreach (var u in state.UnitInstances.Values)
        {
            if (!u.IsAlive) continue;
            if (u.Team != team) continue;

            if (!state.Phase.CommittedThisPhase.Contains(u.Id))
                return false;
        }

        return true;
    }

    // Turn Transition Steps

    private void AdvanceTurn(GameMutationContext ctx, IReadOnlyGameState state)
    {
        var currentTeam = state.Turn.TeamToAct;
        var nextTeam = _session.Context.Teams.GetOpposingTeam(currentTeam);

        var attackerTurnsTaken = state.Turn.AttackerTurnsTaken;
        if (_session.Context.Teams.IsAttacker(currentTeam))
            attackerTurnsTaken++;

        var newTurn = new Domain.Types.Turn(
            attackerTurnsTaken: attackerTurnsTaken,
            teamToAct: nextTeam);

        ctx.Turn.SetTurn(newTurn);
    }

    private void ResolveStartOfTurn(GameMutationContext ctx, IReadOnlyGameState state)
    {
        _effectManager.TickAll(ctx, state);

        // Tick effects may have killed all units on the acting team.
        if (!TeamHasLivingUnits(state, state.Turn.TeamToAct))
        {
            TrySetGameOver();
            return;
        }

        foreach (var u in state.UnitInstances.Values)
        {
            if (!u.IsAlive) continue;
            if (u.Team != state.Turn.TeamToAct) continue;

            ctx.Units.ResetMovePoints(u.Id);
            ctx.Units.ResetActionPoints(u.Id);
        }

        ctx.Turn.ResetActivationPhase();
    }

    private static bool TeamHasLivingUnits(IReadOnlyGameState state, TeamId team)
    {
        foreach (var u in state.UnitInstances.Values)
        {
            if (!u.IsAlive) continue;
            if (u.Team != team) continue;
            return true;
        }

        return false;
    }

    // Simple Helpers

    private GameMutationContext CreateContext(UndoRecord undo)
    {
        return new GameMutationContext(
            _session,
            _rngService,
            undo,
            _combatTelemetry);
    }

    private void Commit(UndoRecord undo)
    {
        _session.Runtime.Undo.Commit(undo);
    }

    private void RecomputeOutcome()
    {
        _session.Runtime.SetGameOutcome(_gameOver.Evaluate(_session));
    }

    private bool TrySetGameOver()
    {
        var outcome = _gameOver.Evaluate(_session);
        if (outcome.Type == GameOutcomeType.Ongoing)
            return false;

        _session.Runtime.SetGameOutcome(outcome);
        return true;
    }
}
