namespace Core.Types;

/// <summary>
/// Strongly-typed identifier for a unit template.
/// </summary>
public readonly struct UnitTemplateId : IEquatable<UnitTemplateId>
{
    public readonly int Value;

    public UnitTemplateId(int value)
    {
        Value = value;
    }

    public bool Equals(UnitTemplateId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is UnitTemplateId other && Equals(other);

    public override int GetHashCode() => Value;

    public override string ToString() => Value.ToString();

    public static bool operator ==(UnitTemplateId left, UnitTemplateId right) =>
        left.Equals(right);

    public static bool operator !=(UnitTemplateId left, UnitTemplateId right) =>
        !left.Equals(right);
}

