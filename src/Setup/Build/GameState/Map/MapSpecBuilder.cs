namespace Setup.Build.GameState.Map;

using Core.Map.Terrain;
using Setup.Build.GameState.Results;
using Setup.Config;
using Setup.Validation;
using Setup.Validation.Primitives;

public sealed class MapSpecBuilder : IMapSpecBuilder
{
    private const double NormalizedTolerance = 0.000001d;

    public MapSpecBuildResult Build(
        MapGenConfig mapGenConfig,
        int seed,
        int rngPosition,
        string configPath)
    {
        ArgumentNullException.ThrowIfNull(mapGenConfig);
        ArgumentNullException.ThrowIfNull(configPath);

        var issues = new ValidationCollector();
        ValidateDimensions(mapGenConfig, configPath, issues);

        if (mapGenConfig.TileDistribution is null)
        {
            issues.Add(ContentIssueFactory.NullCollection(
                ContentSchema.Property(configPath, ContentSchema.Fields.TileDistribution)));
            return new MapSpecBuildResult(null, issues);
        }

        var distribution = ParseTileDistribution(mapGenConfig, configPath, issues, out var totalWeight);
        if (issues.HasErrors)
        {
            return new MapSpecBuildResult(null, issues);
        }

        if (Math.Abs(totalWeight - 1d) > NormalizedTolerance)
        {
            issues.Add(ContentIssueFactory.TileDistributionNotNormalized(
                ContentSchema.Property(configPath, ContentSchema.Fields.TileDistribution),
                totalWeight));
        }

        var mapSpec = new MapSpec(
            width: mapGenConfig.Width,
            height: mapGenConfig.Height,
            seed: seed,
            rngPosition: rngPosition,
            tileDistribution: distribution);

        return new MapSpecBuildResult(mapSpec, issues);
    }

    private static void ValidateDimensions(
        MapGenConfig mapGenConfig,
        string configPath,
        ValidationCollector issues)
    {
        if (mapGenConfig.Width <= 0)
        {
            issues.Add(ContentIssueFactory.InvalidMapDimension(
                ContentSchema.Property(configPath, ContentSchema.Fields.Width),
                mapGenConfig.Width));
        }

        if (mapGenConfig.Height <= 0)
        {
            issues.Add(ContentIssueFactory.InvalidMapDimension(
                ContentSchema.Property(configPath, ContentSchema.Fields.Height),
                mapGenConfig.Height));
        }
    }

    private static Dictionary<TerrainType, double> ParseTileDistribution(
        MapGenConfig mapGenConfig,
        string configPath,
        ValidationCollector issues,
        out double totalWeight)
    {
        totalWeight = 0d;
        var parsed = new Dictionary<TerrainType, double>();

        if (mapGenConfig.TileDistribution.Count == 0)
        {
            issues.Add(ContentIssueFactory.InvalidTileDistribution(
                ContentSchema.Property(configPath, ContentSchema.Fields.TileDistribution),
                "At least one terrain weight is required."));
            return parsed;
        }

        foreach (var (rawTerrain, rawWeight) in mapGenConfig.TileDistribution)
        {
            if (!TryParseTerrain(rawTerrain, out var terrain))
            {
                issues.Add(ContentIssueFactory.InvalidTileDistribution(
                    ContentSchema.Property(configPath, ContentSchema.Fields.TileDistribution),
                    $"Unknown terrain type '{rawTerrain}'."));
                continue;
            }

            if (!double.IsFinite(rawWeight) || rawWeight <= 0d)
            {
                issues.Add(ContentIssueFactory.InvalidTileDistribution(
                    ContentSchema.Property(configPath, ContentSchema.Fields.TileDistribution),
                    $"Weight for terrain '{rawTerrain}' must be finite and greater than 0. Actual value: {rawWeight}."));
                continue;
            }

            if (!parsed.TryAdd(terrain, rawWeight))
            {
                issues.Add(ContentIssueFactory.InvalidTileDistribution(
                    ContentSchema.Property(configPath, ContentSchema.Fields.TileDistribution),
                    $"Duplicate terrain entry '{rawTerrain}'."));
                continue;
            }

            totalWeight += rawWeight;
        }

        if (parsed.Count == 0 || totalWeight <= 0d)
        {
            issues.Add(ContentIssueFactory.InvalidTileDistribution(
                ContentSchema.Property(configPath, ContentSchema.Fields.TileDistribution),
                "No valid terrain weights were provided."));
        }

        return parsed;
    }

    private static bool TryParseTerrain(string? rawTerrain, out TerrainType terrain)
    {
        terrain = default;
        if (string.IsNullOrWhiteSpace(rawTerrain))
        {
            return false;
        }

        return Enum.TryParse(rawTerrain, ignoreCase: true, out terrain);
    }
}
