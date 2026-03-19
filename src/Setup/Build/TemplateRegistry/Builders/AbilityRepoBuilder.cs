namespace Setup.Build.TemplateRegistry.Builders;

using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Effects.Templates;
using Core.Domain.Types;
using Setup.Config;
using Setup.Validation.Primitives;

internal static class AbilityRepoBuilder
{
    public static Dictionary<AbilityId, Ability> Build(
        IReadOnlyList<AbilityConfig> configs,
        IReadOnlyDictionary<EffectTemplateId, EffectTemplate> builtEffects,
        ValidationCollector issues)
    {
        var result = new Dictionary<AbilityId, Ability>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < configs.Count; i++)
        {
            var path = $"Abilities[{i}]";
            var config = configs[i];

            if (!seenIds.Add(config.Id))
            {
                issues.Add(ContentIssueFactory.DuplicateId($"{path}.Id", config.Id));
                continue;
            }

            if (!TryParseAbilityCategoryAndTarget(config, i, issues, out AbilityCategory category, out TargetType targetType))
            {
                continue;
            }

            var effectId = new EffectTemplateId(config.EffectTemplateId);
            if (!builtEffects.ContainsKey(effectId))
            {
                issues.Add(ContentIssueFactory.UnknownReference(
                    $"{path}.EffectTemplateId",
                    "effect template",
                    config.EffectTemplateId));
                continue;
            }

            var targetingConfig = config.Targeting!;
            var targeting = new TargetingRules(
                targetingConfig.Range,
                targetingConfig.RequiresLineOfSight,
                targetType,
                targetingConfig.Radius);

            var id = new AbilityId(config.Id);
            result[id] = new Ability(
                id,
                config.Name,
                category,
                config.ManaCost,
                targeting,
                effectId);
        }

        return result;
    }

    private static bool TryParseAbilityCategoryAndTarget(
        AbilityConfig config,
        int index,
        ValidationCollector issues,
        out AbilityCategory category,
        out TargetType targetType)
    {
        var hasCategory = TryParseEnum(
            config.Category,
            $"Abilities[{index}].Category",
            nameof(AbilityCategory),
            issues,
            out category);

        var hasTarget = TryParseEnum(
            config.Targeting!.AllowedTarget,
            $"Abilities[{index}].Targeting.AllowedTarget",
            nameof(TargetType),
            issues,
            out targetType);

        return hasCategory && hasTarget;
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
