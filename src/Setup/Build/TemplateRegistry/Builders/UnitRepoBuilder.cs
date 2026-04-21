namespace Setup.Build.TemplateRegistry.Builders;

using Core.Domain.Abilities;
using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Templates;
using Setup.Config;
using Setup.Validation;
using Setup.Validation.Primitives;

internal static class UnitRepoBuilder
{
    public static Dictionary<UnitTemplateId, UnitTemplate> Build(
        IReadOnlyList<UnitTemplateConfig> configs,
        IReadOnlyDictionary<AbilityId, Ability> builtAbilities,
        ValidationCollector issues)
    {
        var result = new Dictionary<UnitTemplateId, UnitTemplate>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < configs.Count; i++)
        {
            var path = ContentSchema.UnitTemplate(i);
            var config = configs[i];

            if (!seenIds.Add(config.Id))
            {
                issues.Add(ContentIssueFactory.DuplicateId(ContentSchema.Property(path, ContentSchema.Fields.Id), config.Id));
                continue;
            }

            if (!TryParseRoles(config, path, issues, out var primaryRole, out var secondaryRole))
            {
                continue;
            }

            var abilityIds = new List<AbilityId>();
            for (var j = 0; j < config.AbilityIds.Count; j++)
            {
                var rawId = config.AbilityIds[j];

                var abilityId = new AbilityId(rawId);
                if (!builtAbilities.ContainsKey(abilityId))
                {
                    issues.Add(ContentIssueFactory.UnknownReference(
                        ContentSchema.IndexedProperty(path, ContentSchema.Fields.AbilityIds, j),
                        "ability",
                        rawId));
                    continue;
                }

                abilityIds.Add(abilityId);
            }

            var baseStats = new UnitBaseStats(
                config.MaxHP,
                config.MaxManaPoints,
                config.MovePoints,
                config.PhysicalDamageReceived,
                config.MagicDamageReceived);

            var id = new UnitTemplateId(config.Id);
            result[id] = new UnitTemplate(
                id,
                config.Name,
                primaryRole,
                secondaryRole,
                baseStats,
                abilityIds.ToArray());
        }

        return result;
    }

    private static bool TryParseRoles(
        UnitTemplateConfig config,
        string path,
        ValidationCollector issues,
        out RoleType primaryRole,
        out RoleType? secondaryRole)
    {
        var hasPrimary = TryParseEnum(
            config.PrimaryRole,
            ContentSchema.Property(path, ContentSchema.Fields.PrimaryRole),
            nameof(RoleType),
            issues,
            out primaryRole);

        secondaryRole = null;
        if (string.IsNullOrWhiteSpace(config.SecondaryRole))
        {
            return hasPrimary;
        }

        var hasSecondary = TryParseEnum(
            config.SecondaryRole,
            ContentSchema.Property(path, ContentSchema.Fields.SecondaryRole),
            nameof(RoleType),
            issues,
            out RoleType parsedSecondaryRole);

        if (hasSecondary)
        {
            secondaryRole = parsedSecondaryRole;
        }

        return hasPrimary && hasSecondary;
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
