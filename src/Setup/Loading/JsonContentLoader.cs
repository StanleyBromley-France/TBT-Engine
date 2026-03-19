namespace Setup.Loading;

using Setup.Config;
using Setup.Validation.Primitives;
using System.Text.Json;

public sealed class JsonContentLoader
{
    private static readonly string[] UnitTemplateFiles = ["unitTemplates.json", "units.json"];
    private static readonly string[] AbilityFiles = ["abilities.json"];
    private static readonly string[] EffectTemplateFiles = ["effectTemplates.json", "effects.json"];
    private static readonly string[] EffectComponentTemplateFiles = ["effectComponentTemplates.json", "effectComponents.json"];
    private static readonly string[] GameStateFiles = ["gameStates.json"];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public ContentPack LoadFromFiles(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Content path must not be null or whitespace.", nameof(path));
        }

        var issues = new ValidationCollector();
        var directoryPath = Path.GetFullPath(path);
        if (!Directory.Exists(directoryPath))
        {
            issues.Add(ContentIssueFactory.ContentDirectoryNotFound(directoryPath));
            return new ContentPack
            {
                Issues = issues.Issues.ToList()
            };
        }

        var pack = new ContentPack
        {
            UnitTemplates = LoadList<UnitTemplateConfig>(directoryPath, "UnitTemplates", UnitTemplateFiles, issues),
            Abilities = LoadList<AbilityConfig>(directoryPath, "Abilities", AbilityFiles, issues),
            EffectTemplates = LoadList<EffectTemplateConfig>(directoryPath, "EffectTemplates", EffectTemplateFiles, issues),
            EffectComponentTemplates = LoadList<EffectComponentTemplateConfig>(directoryPath, "EffectComponentTemplates", EffectComponentTemplateFiles, issues),
            GameStates = LoadList<GameStateConfig>(directoryPath, "GameStates", GameStateFiles, issues)
        };

        ValidateRequiredFields(pack, issues);
        pack.Issues = issues.Issues.ToList();
        return pack;
    }

    public T LoadJson<T>(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("File path must not be null or whitespace.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"JSON file not found: {fullPath}", fullPath);
        }

        try
        {
            var json = File.ReadAllText(fullPath);
            var value = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            if (value is null)
            {
                throw new InvalidDataException($"JSON file '{fullPath}' deserialized to null for type '{typeof(T).Name}'.");
            }

            return value;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"JSON in '{fullPath}' is invalid for type '{typeof(T).Name}'.", ex);
        }
    }

    private static List<T> LoadList<T>(
        string directoryPath,
        string collectionPath,
        IReadOnlyList<string> fileNames,
        ValidationCollector issues)
    {
        var filePath = ResolveFilePath(directoryPath, fileNames);
        if (filePath is null)
        {
            issues.Add(ContentIssueFactory.ContentFileNotFound(collectionPath, fileNames));
            return new List<T>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var value = JsonSerializer.Deserialize<List<T>>(json, SerializerOptions);
            if (value is null)
            {
                issues.Add(ContentIssueFactory.InvalidJson(collectionPath, $"List<{typeof(T).Name}>", "Deserializer returned null."));
                return new List<T>();
            }

            return value;
        }
        catch (JsonException ex)
        {
            issues.Add(ContentIssueFactory.InvalidJson(collectionPath, $"List<{typeof(T).Name}>", ex.Message));
            return new List<T>();
        }
        catch (IOException ex)
        {
            issues.Add(ContentIssueFactory.ContentReadError(collectionPath, ex.Message));
            return new List<T>();
        }
    }

    private static string? ResolveFilePath(string directoryPath, IEnumerable<string> fileNames)
    {
        foreach (var fileName in fileNames)
        {
            var path = Path.Combine(directoryPath, fileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static void ValidateRequiredFields(ContentPack pack, ValidationCollector issues)
    {
        ValidateUnitTemplates(pack.UnitTemplates, issues);
        ValidateAbilities(pack.Abilities, issues);
        ValidateEffectTemplates(pack.EffectTemplates, issues);
        ValidateEffectComponentTemplates(pack.EffectComponentTemplates, issues);
        ValidateGameStates(pack.GameStates, issues);
    }

    private static void ValidateUnitTemplates(
        List<UnitTemplateConfig> unitTemplates,
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

    private static void ValidateAbilities(
        List<AbilityConfig> abilities,
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

    private static void ValidateEffectTemplates(
        List<EffectTemplateConfig> effectTemplates,
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

    private static void ValidateEffectComponentTemplates(
        List<EffectComponentTemplateConfig> effectComponentTemplates,
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

    private static void ValidateGameStates(
        List<GameStateConfig> gameStates,
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
