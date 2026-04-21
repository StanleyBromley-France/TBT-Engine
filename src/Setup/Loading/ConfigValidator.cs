using Setup.Config;
using Setup.Validation;
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
            var path = ContentSchema.UnitTemplate(i);
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, ContentSchema.Property(path, ContentSchema.Fields.Id), ContentSchema.Fields.Id, issues);
            ValidateRequiredString(item.Name, ContentSchema.Property(path, ContentSchema.Fields.Name), ContentSchema.Fields.Name, issues);
            ValidateRequiredString(
                item.PrimaryRole,
                ContentSchema.Property(path, ContentSchema.Fields.PrimaryRole),
                ContentSchema.Fields.PrimaryRole,
                issues);

            if (item.AbilityIds is null)
            {
                issues.Add(ContentIssueFactory.NullCollection(ContentSchema.Property(path, ContentSchema.Fields.AbilityIds)));
            }
            else
            {
                for (var j = 0; j < item.AbilityIds.Count; j++)
                {
                    ValidateRequiredString(
                        item.AbilityIds[j],
                        ContentSchema.IndexedProperty(path, ContentSchema.Fields.AbilityIds, j),
                        $"{ContentSchema.Fields.AbilityIds}[]",
                        issues);
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
            var path = ContentSchema.Ability(i);
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, ContentSchema.Property(path, ContentSchema.Fields.Id), ContentSchema.Fields.Id, issues);
            ValidateRequiredString(item.Name, ContentSchema.Property(path, ContentSchema.Fields.Name), ContentSchema.Fields.Name, issues);
            ValidateRequiredString(item.Category, ContentSchema.Property(path, ContentSchema.Fields.Category), ContentSchema.Fields.Category, issues);
            ValidateRequiredString(item.EffectTemplateId, ContentSchema.Property(path, ContentSchema.Fields.EffectTemplateId), ContentSchema.Fields.EffectTemplateId, issues);
            ValidateRequiredString(
                item.Targeting?.AllowedTarget,
                ContentSchema.Property(ContentSchema.Property(path, nameof(AbilityConfig.Targeting)), ContentSchema.Fields.AllowedTarget),
                ContentSchema.Fields.AllowedTarget,
                issues);
        }
    }

    public static void ValidateEffects(
        IReadOnlyList<EffectTemplateConfig> effectTemplates,
        ValidationCollector issues)
    {
        for (var i = 0; i < effectTemplates.Count; i++)
        {
            var item = effectTemplates[i];
            var path = ContentSchema.EffectTemplate(i);
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, ContentSchema.Property(path, ContentSchema.Fields.Id), ContentSchema.Fields.Id, issues);
            ValidateRequiredString(item.Name, ContentSchema.Property(path, ContentSchema.Fields.Name), ContentSchema.Fields.Name, issues);

            if (item.ComponentTemplateIds is null)
            {
                issues.Add(ContentIssueFactory.NullCollection(ContentSchema.Property(path, ContentSchema.Fields.ComponentTemplateIds)));
            }
            else
            {
                for (var j = 0; j < item.ComponentTemplateIds.Count; j++)
                {
                    ValidateRequiredString(
                        item.ComponentTemplateIds[j],
                        ContentSchema.IndexedProperty(path, ContentSchema.Fields.ComponentTemplateIds, j),
                        $"{ContentSchema.Fields.ComponentTemplateIds}[]",
                        issues);
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
            var path = ContentSchema.EffectComponentTemplate(i);
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, ContentSchema.Property(path, ContentSchema.Fields.Id), ContentSchema.Fields.Id, issues);
            ValidateRequiredString(item.Type, ContentSchema.Property(path, ContentSchema.Fields.Type), ContentSchema.Fields.Type, issues);
        }
    }

    public static void ValidateGameStates(
        IReadOnlyList<GameStateConfig> gameStates,
        ValidationCollector issues)
    {
        for (var i = 0; i < gameStates.Count; i++)
        {
            var item = gameStates[i];
            var path = ContentSchema.GameState(i);
            if (item is null)
            {
                issues.Add(ContentIssueFactory.NullItem(path));
                continue;
            }

            ValidateRequiredString(item.Id, ContentSchema.Property(path, ContentSchema.Fields.Id), ContentSchema.Fields.Id, issues);

            if (item.MapGen is null)
            {
                issues.Add(ContentIssueFactory.RequiredField(ContentSchema.Property(path, ContentSchema.Fields.MapGen), ContentSchema.Fields.MapGen));
            }
            else if (item.MapGen.TileDistribution is null)
            {
                issues.Add(ContentIssueFactory.NullCollection(
                    ContentSchema.Property(
                        ContentSchema.Property(path, ContentSchema.Fields.MapGen),
                        ContentSchema.Fields.TileDistribution)));
            }

            if (item.Units is null)
            {
                issues.Add(ContentIssueFactory.NullCollection(ContentSchema.Property(path, ContentSchema.Collections.Units)));
            }
            else
            {
                for (var j = 0; j < item.Units.Count; j++)
                {
                    var spawnPath = ContentSchema.GameStateUnit(path, j);
                    var spawn = item.Units[j];
                    if (spawn is null)
                    {
                        issues.Add(ContentIssueFactory.NullItem(spawnPath));
                        continue;
                    }

                    ValidateRequiredString(
                        spawn.Id,
                        ContentSchema.Property(spawnPath, ContentSchema.Fields.Id),
                        ContentSchema.Fields.Id,
                        issues);
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
