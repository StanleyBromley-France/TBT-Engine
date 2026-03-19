namespace Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders.Parsing;

using Core.Domain.Effects.Stats;
using Core.Domain.Types;
using Setup.Validation.Primitives;

internal static class EffectComponentContentParser
{
    public static bool TryRequireInt(
        int? value,
        string path,
        string fieldName,
        ValidationCollector issues,
        out int parsed)
    {
        if (!value.HasValue)
        {
            issues.Add(ContentIssueFactory.MissingComponentField(path, fieldName));
            parsed = default;
            return false;
        }

        parsed = value.Value;
        return true;
    }

    public static bool TryParseDamageType(
        string? raw,
        string path,
        ValidationCollector issues,
        out DamageType parsed)
    {
        return TryParseEnum(raw, path, nameof(DamageType), issues, out parsed);
    }

    public static bool TryParseStatType(
        string? raw,
        string path,
        ValidationCollector issues,
        out StatType parsed)
    {
        return TryParseEnum(raw, path, nameof(StatType), issues, out parsed);
    }

    private static bool TryParseEnum<TEnum>(
        string? raw,
        string path,
        string enumName,
        ValidationCollector issues,
        out TEnum parsed)
        where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(raw) &&
            Enum.TryParse(raw, ignoreCase: true, out TEnum parsedValue))
        {
            parsed = parsedValue;
            return true;
        }

        issues.Add(ContentIssueFactory.InvalidEnumValue(path, enumName, raw));
        parsed = default;
        return false;
    }
}
