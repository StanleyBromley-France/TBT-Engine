namespace Core.Game;

/// <summary>
/// Stateless deterministic RNG that derives values from an immutable
/// <see cref="RngState"/> and returns an updated state.
/// </summary>
public sealed class DeterministicRng
{
    public (int Value, RngState NextState) Next(RngState state)
    {
        unchecked
        {
            // LCG using seed + position
            int value = state.Seed * 1664525 + 1013904223 + state.Position;
            var nextState = state.Advance(1);
            return (value, nextState);
        }
    }
}


