namespace Setup.Tests.Build.GameState;

using Core.Map.Terrain;
using Setup.Build.GameState.Map;
using Setup.Config;
using Setup.Validation.Primitives;

public sealed class MapSpecBuilderTests
{
    [Fact]
    public void Build_Valid_Config_Builds_MapSpec()
    {
        var builder = new MapSpecBuilder();
        var config = new MapGenConfig
        {
            Width = 4,
            Height = 3,
            TileDistribution = new Dictionary<string, double>
            {
                ["Plain"] = 0.7,
                ["Mountain"] = 0.2,
                ["Water"] = 0.1
            }
        };

        var result = builder.Build(config, configPath: "GameStates[0].MapGen");

        Assert.NotNull(result.MapSpec);
        Assert.False(result.HasErrors);
        Assert.Equal(4, result.MapSpec!.Width);
        Assert.Equal(3, result.MapSpec.Height);
        Assert.Equal(3, result.MapSpec.TileDistribution.Count);
        Assert.Equal(0.7, result.MapSpec.TileDistribution[TerrainType.Plain]);
        Assert.Equal(0.2, result.MapSpec.TileDistribution[TerrainType.Mountain]);
        Assert.Equal(0.1, result.MapSpec.TileDistribution[TerrainType.Water]);
    }

    [Fact]
    public void Build_Invalid_Dimensions_Returns_Errors_And_Null_MapSpec()
    {
        var builder = new MapSpecBuilder();
        var config = new MapGenConfig
        {
            Width = 0,
            Height = -2,
            TileDistribution = new Dictionary<string, double>
            {
                ["Plain"] = 1.0
            }
        };

        var result = builder.Build(config, configPath: "GameStates[0].MapGen");

        Assert.Null(result.MapSpec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i => i.Code == ContentIssueFactory.InvalidMapDimensionCode && i.Path == "GameStates[0].MapGen.Width");
        Assert.Contains(result.IssueView.Issues, i => i.Code == ContentIssueFactory.InvalidMapDimensionCode && i.Path == "GameStates[0].MapGen.Height");
    }

    [Fact]
    public void Build_Unknown_Terrain_In_Distribution_Returns_Error()
    {
        var builder = new MapSpecBuilder();
        var config = new MapGenConfig
        {
            Width = 2,
            Height = 2,
            TileDistribution = new Dictionary<string, double>
            {
                ["Lava"] = 1.0
            }
        };

        var result = builder.Build(config, configPath: "GameStates[0].MapGen");

        Assert.Null(result.MapSpec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i => i.Code == ContentIssueFactory.InvalidTileDistributionCode);
    }

    [Fact]
    public void Build_Non_Positive_Or_Non_Finite_Weights_Return_Error()
    {
        var builder = new MapSpecBuilder();
        var config = new MapGenConfig
        {
            Width = 2,
            Height = 2,
            TileDistribution = new Dictionary<string, double>
            {
                ["Plain"] = 1.0,
                ["Mountain"] = 0.0,
                ["Water"] = double.NaN
            }
        };

        var result = builder.Build(config, configPath: "GameStates[0].MapGen");

        Assert.Null(result.MapSpec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i => i.Code == ContentIssueFactory.InvalidTileDistributionCode);
    }

    [Fact]
    public void Build_Empty_Distribution_Returns_Error()
    {
        var builder = new MapSpecBuilder();
        var config = new MapGenConfig
        {
            Width = 2,
            Height = 2,
            TileDistribution = new Dictionary<string, double>()
        };

        var result = builder.Build(config, configPath: "GameStates[0].MapGen");

        Assert.Null(result.MapSpec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i => i.Code == ContentIssueFactory.InvalidTileDistributionCode);
    }

    [Fact]
    public void Build_Non_Normalized_Distribution_Emits_Warning_And_Still_Builds()
    {
        var builder = new MapSpecBuilder();
        var config = new MapGenConfig
        {
            Width = 2,
            Height = 2,
            TileDistribution = new Dictionary<string, double>
            {
                ["Plain"] = 2.0,
                ["Mountain"] = 1.0
            }
        };

        var result = builder.Build(config, configPath: "GameStates[0].MapGen");

        Assert.NotNull(result.MapSpec);
        Assert.False(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i => i.Code == ContentIssueFactory.TileDistributionNormalizationWarningCode);
        Assert.Contains(result.IssueView.Issues, i => i.Severity == ContentIssueSeverity.Warning);
    }
}
