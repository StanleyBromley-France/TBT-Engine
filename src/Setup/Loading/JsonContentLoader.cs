namespace Setup.Loading;

using Setup.Config;
using Setup.Validation;
using Setup.Validation.Primitives;
using System.Text.Json;

public static class JsonContentLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ContentPack LoadFromFiles(string path)
    {
        var pack = new ContentPack();
        IContentPackBuilder builder = pack;

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Content path must not be null or whitespace.", nameof(path));
        }

        var issues = new ValidationCollector();
        var directoryPath = Path.GetFullPath(path);
        if (!Directory.Exists(directoryPath))
        {
            issues.Add(ContentIssueFactory.ContentDirectoryNotFound(directoryPath));
            builder.AddIssues(issues);

            return pack;
        }

        var gamestates = LoadList<GameStateConfig>(directoryPath, ContentSchema.Collections.GameStates, ContentFiles.GameStateFiles, issues);
        ConfigValidator.ValidateGameStates(gamestates, issues);

        builder.AddGameStates(gamestates);

        IContentPackTemplatesBuilder templatesBuilder = builder.ContentPackTemplatesBuilder;

        var units = LoadList<UnitTemplateConfig>(directoryPath, ContentSchema.Collections.UnitTemplates, ContentFiles.UnitTemplateFiles, issues);
        var abilities = LoadList<AbilityConfig>(directoryPath, ContentSchema.Collections.Abilities, ContentFiles.AbilityFiles, issues);
        var effects = LoadList<EffectTemplateConfig>(directoryPath, ContentSchema.Collections.EffectTemplates, ContentFiles.EffectTemplateFiles, issues);
        var effectComps = LoadList<EffectComponentTemplateConfig>(directoryPath, ContentSchema.Collections.EffectComponentTemplates, ContentFiles.EffectComponentTemplateFiles, issues);

        ConfigValidator.ValidateUnits(units, issues);
        ConfigValidator.ValidateAbilities(abilities, issues);
        ConfigValidator.ValidateEffects(effects, issues);
        ConfigValidator.ValidateEffectComponents(effectComps, issues);

        templatesBuilder.AddUnits(units);
        templatesBuilder.AddAbilities(abilities);
        templatesBuilder.AddEffects(effects);
        templatesBuilder.AddEffectComponents(effectComps);

        builder.AddIssues(issues);

        return pack;
    }

    public static T LoadJson<T>(string path)
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
}
