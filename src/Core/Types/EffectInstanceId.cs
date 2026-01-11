namespace Core.Types;

/// <summary>
/// Strongly-typed identifier for a runtime effect instance.
/// </summary>
public readonly struct EffectInstanceId : IEquatable<EffectInstanceId>
{
    public readonly int Value;

    public EffectInstanceId(int value)
    {
        Value = value;
    }

    public bool Equals(EffectInstanceId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is EffectInstanceId other && Equals(other);

    public override int GetHashCode() => Value;

    public override string ToString() => Value.ToString();

    public static bool operator ==(EffectInstanceId left, EffectInstanceId right) =>
        left.Equals(right);

    public static bool operator !=(EffectInstanceId left, EffectInstanceId right) =>
        !left.Equals(right);
}

