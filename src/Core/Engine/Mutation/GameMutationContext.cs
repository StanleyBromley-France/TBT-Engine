namespace Core.Engine.Mutation;

using Core.Domain.Effects.Instances.Mutable;
using Core.Engine.Mutation.Mutators;
using Core.Engine.Random;
using Core.Game.Requests;
using Core.Game.Session;
using Core.Game.State;
using Core.Undo;

/// <summary>
/// Per-operation mutation gateway.
/// Created by EngineFacade for each mutation operation.
/// Holds the session reference + operation undo record,
/// and exposes grouped mutators.
/// </summary>
public sealed class GameMutationContext : IGameMutationAccess
{
    private readonly GameSession _session;
    private readonly DeterministicRng _rngService;
    private readonly UndoRecord _undoRecord;

    public IUnitsMutator Units { get; }
    public IMovementMutator Movement { get; }
    public ITurnMutator Turn { get; }
    public IEffectsMutator Effects { get; }
    public IRngMutator Rng { get; }

    internal GameMutationContext(GameSession session, DeterministicRng rng, UndoRecord undoRecord)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _rngService = rng ?? throw new ArgumentNullException(nameof(rng));
        _undoRecord = undoRecord;

        Units = new UnitsMutator(this);
        Movement = new MovementMutator(this);
        Turn = new TurnMutator(this);
        Effects = new EffectsMutator(this);
        Rng = new RngMutator(this);
    }

    internal GameMutationContext(
    GameSession session,
    DeterministicRng rng,
    UndoRecord undoRecord,
    IUnitsMutator units,
    IMovementMutator movement,
    ITurnMutator turn,
    IEffectsMutator effects,
    IRngMutator rngMutator)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _rngService = rng ?? throw new ArgumentNullException(nameof(rng));
        _undoRecord = undoRecord;

        Units = units ?? throw new ArgumentNullException(nameof(units));
        Movement = movement ?? throw new ArgumentNullException(nameof(movement));
        Turn = turn ?? throw new ArgumentNullException(nameof(turn));
        Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        Rng = rngMutator ?? throw new ArgumentNullException(nameof(rngMutator));
    }

    public EffectInstance CreateEffect(CreateEffectRequest request) => _session.Context.SessionServices.CreateEffect(request);

    GameState IGameMutationAccess.GetState() => _session.Runtime.State;
    UndoRecord IGameMutationAccess.GetUndo() => _undoRecord;
    DeterministicRng IGameMutationAccess.GetRngService() => _rngService;
}
