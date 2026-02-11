namespace Core.Domain.Types;

/// <summary>
/// Strongly-typed identifier for an effect component instance.
/// </summary>
public readonly struct EffectComponentInstanceId : IEquatable<EffectComponentInstanceId>
{
    public readonly string Value;

    public EffectComponentInstanceId(string value)
    {
        Value = value;
    }

    public bool Equals(EffectComponentInstanceId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is EffectComponentInstanceId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(EffectComponentInstanceId left, EffectComponentInstanceId right) =>
        left.Equals(right);

    public static bool operator !=(EffectComponentInstanceId left, EffectComponentInstanceId right) =>
        !left.Equals(right);
}

