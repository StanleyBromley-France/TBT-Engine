namespace Core.Engine.Mutation.Mutators;

using Random;

/// <summary>
/// Mutation-layer API for managing random number genaration using RngState.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="DeterministicRng"/> and ensures that all RNG state changes
/// are applied through <see cref="GameMutationContext"/>.
/// </para>
/// <para>
/// Each roll advances the stored RNG state inside <see cref="Core.Game.GameState"/>,
/// guaranteeing deterministic and replayable outcomes.
/// </para>
/// Intended to be used exclusively by engine rules and effects through
/// the mutation pipeline.
/// </remarks>
public sealed class RngMutator
{
    private readonly IGameMutationAccess _ctx;
    private readonly DeterministicRng _rng;

    public RngMutator(GameMutationContext ctx, DeterministicRng rng = null)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _rng = rng ?? new DeterministicRng();
    }

    public int RollRandom(int exclusiveMax) => RollRandom(0, exclusiveMax);

    public int RollRandom(int inclusiveMin, int exclusiveMax)
    {
        if (exclusiveMax <= inclusiveMin)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "exclusiveMax must be > inclusiveMin");

        int raw = AdvanceRngAndGetRaw();

        int range = exclusiveMax - inclusiveMin;

        uint u = unchecked((uint)raw);
        int offset = (int)(u % (uint)range);

        return inclusiveMin + offset;
    }

    private int AdvanceRngAndGetRaw()
    {
        var state = _ctx.GetState();

        var before = state.Rng;
        var (raw, nextState) = _rng.Next(before);

        state.Rng = nextState;

        // TODO: Record undo step in UndoRecord

        return raw;
    }
}
