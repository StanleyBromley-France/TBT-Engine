namespace Core.Engine;

using Core.Domain.Repositories;
using Core.Engine.Actions.Choice;
using Core.Engine.Effects;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Engine.Rules;
using Core.Engine.Turn;
using Core.Engine.Undo;
using Core.Game;

/// <summary>
/// Top-level orchestrator and entry point into the simulation.
///
/// Responsibilities:
/// - Defines operation boundaries (action, tick, turn advance)
/// - Creates a GameMutationContext per operation
/// - Routes mutations through rules, turn policy, and effect manager
/// - Exposes read-only queries (state, legal actions, game over)
///
/// All game state mutation flows through GameMutationContext.
/// </summary>
public sealed class EngineFacade
{
    private readonly GameSession _session;
    private readonly IGameRules _rules;
    private readonly ITurnPolicy _policy;
    private readonly DeterministicRng _rngService;
    private readonly EffectManager _effectManager;

    internal EngineFacade(
        GameSession session,
        IGameRules rules,
        ITurnPolicy policy,
        DeterministicRng rngService,
        EffectManager effectManager)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _rngService = rngService ?? throw new ArgumentNullException(nameof(rngService));
        _effectManager = effectManager ?? throw new ArgumentNullException(nameof(effectManager));
    }

    public TemplateRegistry GetContent()
    {
        return _session.Content;
    }

    public IReadOnlyGameState GetState()
    {
        return _session.State;
    }

    public IEnumerable<ActionChoice> GetLegalActions()
    {
        return _rules.GetLegalActions(_session.State);
    }

    public void ApplyAction(ActionChoice action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var undo = new UndoRecord();
        var context = CreateContext(undo);

        //logic

        Commit(undo);
    }

    public void AdvanceTurn()
    {
        var undo = new UndoRecord();
        var context = CreateContext(undo);

        //logic

        Commit(undo);
    }

    public void ResolveEndOfTurn()
    {
        var undo = new UndoRecord();
        var context = CreateContext(undo);

        // logic

        Commit(undo);
    }

    public void ResolveStartOfTurn()
    {
        var undo = new UndoRecord();
        var context = CreateContext(undo);

        // logic

        Commit(undo);
    }

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