namespace Core.Engine.Mutation;

using Core.Engine.Mutation.Mutators;
using Core.Engine.Random;
using Core.Game;
//using Core.Undo;

/// <summary>
/// Per-operation mutation gateway.
/// Created by EngineFacade for each mutation operation.
/// Holds the session reference + operation undo record,
/// and exposes grouped mutators.
/// </summary>
public sealed class GameMutationContext : IGameMutationAccess
{
    // TODO: Add Undo reference

    private readonly GameSession _session;
    private readonly DeterministicRng _rngService;

    public UnitsMutator Units { get; }
    public MovementMutator Movement { get; }
    public TurnMutator Turn { get; }
    public EffectsMutator Effects { get; }
    public RngMutator Rng { get; }

    public GameMutationContext(GameSession session, DeterministicRng rng)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _rngService = rng ?? throw new ArgumentNullException(nameof(rng));

        Units = new UnitsMutator(this);
        Movement = new MovementMutator(this);
        Turn = new TurnMutator(this);
        Effects = new EffectsMutator(this);
        Rng = new RngMutator(this);
    }

    GameState IGameMutationAccess.GetState() => _session.State;
    DeterministicRng IGameMutationAccess.GetRngService() => _rngService;
}
