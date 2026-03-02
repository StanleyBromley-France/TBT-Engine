namespace Core.Engine;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Effects;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Engine.Rules;
using Core.Engine.Undo;
using Core.Game;

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
    private readonly EffectManager _effectManager;

    internal EngineFacade(
        GameSession session,
        IActionRules rules,
        IActionDispatcher dispatcher,
        DeterministicRng rngService,
        EffectManager effectManager)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _rngService = rngService ?? throw new ArgumentNullException(nameof(rngService));
        _effectManager = effectManager ?? throw new ArgumentNullException(nameof(effectManager));
    }

    public TemplateRegistry GetContent() => _session.Content;

    public IReadOnlyGameState GetState() => _session.State;

    public IEnumerable<ActionChoice> GetLegalActions()
        => _rules.Generator.GetLegalActions(_session.State);

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

        var state = _session.State;

        if (!_rules.Validator.IsActionLegal(state, action))
            throw new InvalidOperationException("Illegal action.");

        var undo = new UndoRecord();
        var ctx = CreateContext(undo);

        _dispatcher.Execute(state, ctx, action);

        ResolvePostAction(ctx, state);

        Commit(undo);
    }

    // Post apply action resolution

    private void ResolvePostAction(GameMutationContext ctx, IReadOnlyGameState state)
    {
        if (ShouldAdvanceTurn(state))
        {
            AdvanceTurn(ctx, state);
            ResolveStartOfTurn(ctx, state);
        }

        // TODO: Add Win con
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
        var newTeam = _session.Teams.GetOpposingTeam(state.Turn.TeamToAct);
        var newTurn = new Domain.Types.Turn(turnNumber: state.Turn.TurnNumber + 1, newTeam);
        ctx.Turn.SetTurn(newTurn);
    }

    private void ResolveStartOfTurn(GameMutationContext ctx, IReadOnlyGameState state)
    {
        _effectManager.TickAll(ctx, state);

        foreach (var u in state.UnitInstances.Values)
        {
            if (!u.IsAlive) continue;
            if (u.Team != state.Turn.TeamToAct) continue;

            ctx.Units.ResetMovePoints(u.Id);
            ctx.Units.ResetActionPoints(u.Id);
        }

        var unitId = FindNewActiveUnit(state);

        ctx.Turn.ResetActivationPhaseAndSetNew(unitId);
    }

    private static UnitInstanceId FindNewActiveUnit(IReadOnlyGameState state)
    {
        var team = state.Turn.TeamToAct;

        foreach (var u in state.UnitInstances.Values)
        {
            if (!u.IsAlive) continue;
            if (u.Team != team) continue;

            return u.Id;
        }

        throw new InvalidOperationException(
            $"No eligible active unit found for team {team}. " +
            "Game-over should have been resolved before start-of-turn.");
    }

    // Simple Helpers

    private GameMutationContext CreateContext(UndoRecord undo)
    {
        return new GameMutationContext(
            _session,
            _rngService,
            undo);
    }

    private void Commit(UndoRecord undo)
    {
        _session.Undo.Commit(undo);
    }
}