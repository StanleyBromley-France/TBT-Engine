namespace Core.Types;

/// <summary>
/// Strongly-typed identifier for a runtime unit instance.
/// Distinct from UnitTemplateId.
/// </summary>
public readonly struct UnitInstanceId : IEquatable<UnitInstanceId>
{
    public readonly int Value;

    public UnitInstanceId(int value)
    {
        Value = value;
    }

    public bool Equals(UnitInstanceId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is UnitInstanceId other && Equals(other);

    public override int GetHashCode() => Value;

    public override string ToString() => Value.ToString();

    public static bool operator ==(UnitInstanceId left, UnitInstanceId right) =>
        left.Equals(right);

    public static bool operator !=(UnitInstanceId left, UnitInstanceId right) =>
        !left.Equals(right);
}
