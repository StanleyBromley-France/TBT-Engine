namespace Core.Game.Bootstrap.Builders.Map.Rng;

public sealed class DeterministicMapGenerationRng : IMapGenerationRng
{
    public (int Value, MapGenerationRngState NextState) Next(MapGenerationRngState state)
    {
        unchecked
        {
            int value = state.Seed * 1664525 + 1013904223 + state.Position;
            var nextState = state.Advance(1);
            return (value, nextState);
        }
    }
}
