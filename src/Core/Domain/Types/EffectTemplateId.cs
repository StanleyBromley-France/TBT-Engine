using System;

namespace Core.Domain.Types;

/// <summary>
/// Strongly-typed identifier for an effect template.
/// </summary>
public readonly struct EffectTemplateId : IEquatable<EffectTemplateId>
{
    public readonly string Value;

    public EffectTemplateId(string value)
    {
        Value = value;
    }

    public bool Equals(EffectTemplateId other) => Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is EffectTemplateId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(EffectTemplateId left, EffectTemplateId right) =>
        left.Equals(right);

    public static bool operator !=(EffectTemplateId left, EffectTemplateId right) =>
        !left.Equals(right);
}
