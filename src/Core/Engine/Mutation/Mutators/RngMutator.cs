namespace Core.Engine.Mutation.Mutators;

using Core.Engine.Undo.Steps.Rng;
using Random;

public sealed class RngMutator : IRngMutator
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

        _ctx.GetUndo().AddStep(new RngStateChangeUndo(before));

        return raw;
    }
}
