namespace Core.Types;

/// <summary>
/// Strongly-typed identifier for a unit template.
/// </summary>
public readonly struct UnitTemplateId : IEquatable<UnitTemplateId>
{
    public readonly string Value;

    public UnitTemplateId(string value)
    {
        Value = value;
    }

    public bool Equals(UnitTemplateId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is UnitTemplateId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(UnitTemplateId left, UnitTemplateId right) =>
        left.Equals(right);

    public static bool operator !=(UnitTemplateId left, UnitTemplateId right) =>
        !left.Equals(right);
}

