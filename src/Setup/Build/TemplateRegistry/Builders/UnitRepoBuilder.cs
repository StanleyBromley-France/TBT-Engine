namespace Setup.Build.TemplateRegistry.Builders;

using Core.Domain.Abilities;
using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Templates;
using Setup.Config;
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
            var path = $"UnitTemplates[{i}]";
            var config = configs[i];

            if (!seenIds.Add(config.Id))
            {
                issues.Add(ContentIssueFactory.DuplicateId($"{path}.Id", config.Id));
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
                        $"{path}.AbilityIds[{j}]",
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
                baseStats,
                abilityIds.ToArray());
        }

        return result;
    }
}
