using Setup.Config;
using Setup.Validation.Primitives;

namespace Setup.Loading;

internal static class ConfigValidator
{
    public static void ValidateUnits(
        IReadOnlyList<UnitTemplateConfig> unitTemplates,
        ValidationCollector issues)
    {
        for (var i = 0; i < unitTemplates.Count; i++)
        {
            var item = unitTemplates[i];
            var path = $"UnitTemplates[{i}]";
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, $"{path}.Id", "Id", issues);
            ValidateRequiredString(item.Name, $"{path}.Name", "Name", issues);

            if (item.AbilityIds is null)
            {
                issues.Add(ContentIssueFactory.NullCollection($"{path}.AbilityIds"));
            }
            else
            {
                for (var j = 0; j < item.AbilityIds.Count; j++)
                {
                    ValidateRequiredString(item.AbilityIds[j], $"{path}.AbilityIds[{j}]", "AbilityIds[]", issues);
                }
            }
        }
    }

    public static void ValidateAbilities(
        IReadOnlyList<AbilityConfig> abilities,
        ValidationCollector issues)
    {
        for (var i = 0; i < abilities.Count; i++)
        {
            var item = abilities[i];
            var path = $"Abilities[{i}]";
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, $"{path}.Id", "Id", issues);
            ValidateRequiredString(item.Name, $"{path}.Name", "Name", issues);
            ValidateRequiredString(item.Category, $"{path}.Category", "Category", issues);
            ValidateRequiredString(item.EffectTemplateId, $"{path}.EffectTemplateId", "EffectTemplateId", issues);
            ValidateRequiredString(item.Targeting?.AllowedTarget, $"{path}.Targeting.AllowedTarget", "AllowedTarget", issues);
        }
    }

    public static void ValidateEffects(
        IReadOnlyList<EffectTemplateConfig> effectTemplates,
        ValidationCollector issues)
    {
        for (var i = 0; i < effectTemplates.Count; i++)
        {
            var item = effectTemplates[i];
            var path = $"EffectTemplates[{i}]";
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, $"{path}.Id", "Id", issues);
            ValidateRequiredString(item.Name, $"{path}.Name", "Name", issues);

            if (item.ComponentTemplateIds is null)
            {
                issues.Add(ContentIssueFactory.NullCollection($"{path}.ComponentTemplateIds"));
            }
            else
            {
                for (var j = 0; j < item.ComponentTemplateIds.Count; j++)
                {
                    ValidateRequiredString(item.ComponentTemplateIds[j], $"{path}.ComponentTemplateIds[{j}]", "ComponentTemplateIds[]", issues);
                }
            }
        }
    }

    public static void ValidateEffectComponents(
        IReadOnlyList<EffectComponentTemplateConfig> effectComponentTemplates,
        ValidationCollector issues)
    {
        for (var i = 0; i < effectComponentTemplates.Count; i++)
        {
            var item = effectComponentTemplates[i];
            var path = $"EffectComponentTemplates[{i}]";
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, $"{path}.Id", "Id", issues);
            ValidateRequiredString(item.Type, $"{path}.Type", "Type", issues);
        }
    }

    public static void ValidateGameStates(
        IReadOnlyList<GameStateConfig> gameStates,
        ValidationCollector issues)
    {
        for (var i = 0; i < gameStates.Count; i++)
        {
            var item = gameStates[i];
            var path = $"GameStates[{i}]";
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, $"{path}.Id", "Id", issues);

            if (item.MapGen is null)
            {
                issues.Add(ContentIssueFactory.RequiredField($"{path}.MapGen", "MapGen"));
            }
            else if (item.MapGen.TileDistribution is null)
            {
                issues.Add(ContentIssueFactory.NullCollection($"{path}.MapGen.TileDistribution"));
            }

            if (item.Units is null)
            {
                issues.Add(ContentIssueFactory.NullCollection($"{path}.Units"));
            }
            else
            {
                for (var j = 0; j < item.Units.Count; j++)
                {
                    var spawn = item.Units[j];
                    if (spawn is null)
                    {
                        issues.Add(ContentIssueFactory.NullItem($"{path}.Units[{j}]"));
                        continue;
                    }

                    ValidateRequiredString(spawn.UnitTemplateId, $"{path}.Units[{j}].UnitTemplateId", "UnitTemplateId", issues);
                }
            }
        }
    }

    private static void ValidateRequiredString(
        string? value,
        string path,
        string fieldName,
        ValidationCollector issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(ContentIssueFactory.RequiredField(path, fieldName));
        }
    }
}
