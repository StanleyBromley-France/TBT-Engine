namespace Core.Domain.Types;

/// <summary>
/// Strongly-typed identifier for an ability template.
/// </summary>
public readonly struct AbilityId : IEquatable<AbilityId>
{
    public readonly string Value;

    public AbilityId(string value)
    {
        Value = value;
    }

    public bool Equals(AbilityId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is AbilityId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(AbilityId left, AbilityId right) =>
        left.Equals(right);

    public static bool operator !=(AbilityId left, AbilityId right) =>
        !left.Equals(right);
}
