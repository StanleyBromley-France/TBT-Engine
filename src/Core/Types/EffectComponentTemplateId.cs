namespace Core.Types;

/// <summary>
/// Strongly-typed identifier for an effect component template.
/// </summary>
public readonly struct EffectComponentTemplateId : IEquatable<EffectComponentTemplateId>
{
    public readonly string Value;

    public EffectComponentTemplateId(string value)
    {
        Value = value;
    }

    public bool Equals(EffectComponentTemplateId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is EffectComponentTemplateId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(EffectComponentTemplateId left, EffectComponentTemplateId right) =>
        left.Equals(right);

    public static bool operator !=(EffectComponentTemplateId left, EffectComponentTemplateId right) =>
        !left.Equals(right);
}

