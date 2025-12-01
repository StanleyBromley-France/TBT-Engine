namespace Core.Game;

/// <summary>
/// Holds the deterministic random number generator state used by the simulation.
/// This enables reproducible combat outcomes by embedding RNG progress inside the
/// <see cref="GameState"/>.
/// </summary>
/// <remarks>
/// <para>
/// Random values are derived from the pair (Seed, Position). Each call to the
/// deterministic RNG advances the position and returns a new <see cref="RngState"/>.
/// </para>
/// </remarks>
public sealed class RngState
{
    public int Seed { get; }
    public int Position { get; }

    public RngState(int seed, int position)
    {
        Seed = seed;
        Position = position;
    }

    public RngState Advance(int steps = 1) => new RngState(Seed, Position + steps);
}

