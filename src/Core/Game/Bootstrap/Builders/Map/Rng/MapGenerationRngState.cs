namespace Core.Game.Bootstrap.Builders.Map.Rng;

public sealed class MapGenerationRngState
{
    public int Seed { get; }
    public int Position { get; }

    public MapGenerationRngState(int seed, int position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be non-negative.");
        }

        Seed = seed;
        Position = position;
    }

    public MapGenerationRngState Advance(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Advance amount must be non-negative.");
        }

        return new MapGenerationRngState(Seed, Position + amount);
    }
}
