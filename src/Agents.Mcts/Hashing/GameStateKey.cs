namespace Agents.Mcts.Hashing;

/// <summary>
/// Stable identifier for a fully-specified simulation state.
/// </summary>
public readonly struct GameStateKey : IEquatable<GameStateKey>
{
    public GameStateKey(ulong value)
    {
        Value = value;
    }

    public ulong Value { get; }

    public bool Equals(GameStateKey other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is GameStateKey other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("X16");

    public static bool operator ==(GameStateKey left, GameStateKey right) => left.Equals(right);

    public static bool operator !=(GameStateKey left, GameStateKey right) => !left.Equals(right);
}
