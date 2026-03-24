namespace Core.Tests.Game.Bootstrap.Builders;

using Core.Domain.Types;
using Core.Game.Bootstrap.Builders.Map;
using Core.Game.Bootstrap.Contracts;
using Core.Game.Bootstrap.Builders.Map.Rng;
using Core.Map.Grid;
using Core.Map.Search;
using Core.Map.Terrain;

public sealed class MapBuilderTests
{
    [Fact]
    public void Build_SameSpec_IsDeterministic()
    {
        var builder = new MapBuilder(new DeterministicMapGenerationRng());
        var spec = new MapSpecStub(
            width: 4,
            height: 3,
            tileDistribution: new Dictionary<TerrainType, double>
            {
                [TerrainType.Plain] = 0.7,
                [TerrainType.Mountain] = 0.2,
                [TerrainType.Water] = 0.1
            });

        var first = builder.Build(spec, new MapBuildOptions { Seed = 123 });
        var second = builder.Build(spec, new MapBuildOptions { Seed = 123 });

        Assert.Equal(first.AttemptUsed, second.AttemptUsed);
        Assert.Equal(first.Map.Width, second.Map.Width);
        Assert.Equal(first.Map.Height, second.Map.Height);

        for (var col = 0; col < first.Map.Width; col++)
        {
            for (var row = 0; row < first.Map.Height; row++)
            {
                var coord = HexCoordConverter.FromOffset(col, row);
                Assert.True(first.Map.TryGetTile(coord, out var firstTile));
                Assert.True(second.Map.TryGetTile(coord, out var secondTile));
                Assert.Equal(firstTile.Terrain, secondTile.Terrain);
            }
        }
    }

    [Fact]
    public void Build_DifferentSeed_Produces_Different_Map()
    {
        var builder = new MapBuilder(new DeterministicMapGenerationRng());
        var spec = new MapSpecStub(
            width: 32,
            height: 32,
            tileDistribution: new Dictionary<TerrainType, double>
            {
                [TerrainType.Plain] = 0.45,
                [TerrainType.Mountain] = 0.55
            });

        var first = builder.Build(spec, new MapBuildOptions { Seed = 123 });
        var second = builder.Build(spec, new MapBuildOptions { Seed = 456 });

        var hasDifference = false;
        for (var col = 0; col < first.Map.Width && !hasDifference; col++)
        {
            for (var row = 0; row < first.Map.Height; row++)
            {
                var coord = HexCoordConverter.FromOffset(col, row);
                Assert.True(first.Map.TryGetTile(coord, out var firstTile));
                Assert.True(second.Map.TryGetTile(coord, out var secondTile));
                if (firstTile.Terrain != secondTile.Terrain)
                {
                    hasDifference = true;
                    break;
                }
            }
        }

        Assert.True(hasDifference);
    }

    [Fact]
    public void Build_Empty_Distribution_Throws_InvalidOperation()
    {
        var builder = new MapBuilder(new DeterministicMapGenerationRng());
        var spec = new MapSpecStub(
            width: 2,
            height: 2,
            tileDistribution: new Dictionary<TerrainType, double>());

        Assert.Throws<InvalidOperationException>(() => builder.Build(spec, MapBuildOptions.Default));
    }

    [Fact]
    public void Build_With_RequiredWalkableCoords_Produces_Walkable_Tiles_At_Those_Coords()
    {
        var builder = new MapBuilder(new DeterministicMapGenerationRng());
        var spec = new MapSpecStub(
            width: 6,
            height: 6,
            tileDistribution: new Dictionary<TerrainType, double>
            {
                [TerrainType.Plain] = 0.4,
                [TerrainType.Mountain] = 0.6
            });

        var required = new List<HexCoord>
        {
            new(0, 0),
            new(2, 1),
            new(4, 2)
        };

        var result = builder.Build(spec, new MapBuildOptions
        {
            RequiredWalkableCoords = required,
            MaxAttempts = 512
        });
        var map = result.Map;

        foreach (var coord in required)
        {
            Assert.True(map.TryGetTile(coord, out var tile));
            Assert.True(tile.IsWalkable);
        }

        Assert.InRange(result.AttemptUsed, 0, 511);
    }

    [Fact]
    public void Build_With_Impossible_RequiredWalkableCoords_Throws()
    {
        var builder = new MapBuilder(new DeterministicMapGenerationRng());
        var spec = new MapSpecStub(
            width: 3,
            height: 3,
            tileDistribution: new Dictionary<TerrainType, double>
            {
                [TerrainType.Mountain] = 1.0
            });

        var required = new List<HexCoord> { new(0, 0) };

        Assert.Throws<InvalidOperationException>(() => builder.Build(spec, new MapBuildOptions
        {
            RequiredWalkableCoords = required,
            MaxAttempts = 16
        }));
    }

    [Fact]
    public void Build_With_NonPositive_MaxAttempts_Clamps_To_One_Attempt()
    {
        var builder = new MapBuilder(new DeterministicMapGenerationRng());
        var spec = new MapSpecStub(
            width: 3,
            height: 3,
            tileDistribution: new Dictionary<TerrainType, double>
            {
                [TerrainType.Plain] = 1.0
            });

        var result = builder.Build(spec, new MapBuildOptions
        {
            MaxAttempts = 0
        });

        Assert.Equal(0, result.AttemptUsed);
    }

    [Fact]
    public void Build_With_Connectivity_Requirement_Passes_When_All_Tiles_Are_Walkable()
    {
        var builder = new MapBuilder(new DeterministicMapGenerationRng());
        var spec = new MapSpecStub(
            width: 4,
            height: 4,
            tileDistribution: new Dictionary<TerrainType, double>
            {
                [TerrainType.Plain] = 1.0
            });

        var result = builder.Build(spec, new MapBuildOptions
        {
            RequiredWalkableCoords = [new HexCoord(0, 0), new HexCoord(3, 2)],
            RequireAllRequiredCoordsConnected = true,
            MaxAttempts = 1
        });
        var map = result.Map;

        Assert.True(map.TryGetTile(new HexCoord(0, 0), out var a));
        Assert.True(map.TryGetTile(new HexCoord(3, 2), out var b));
        Assert.True(a.IsWalkable);
        Assert.True(b.IsWalkable);
        Assert.Equal(0, result.AttemptUsed);
    }

    [Fact]
    public void SmoothSingleTileIslands_Converts_Isolated_Center_Tile_To_Surrounding_Terrain()
    {
        var tiles = new Tile[5, 5];
        for (var col = 0; col < 5; col++)
        {
            for (var row = 0; row < 5; row++)
            {
                tiles[col, row] = new Tile { Terrain = TerrainType.Plain };
            }
        }

        // Center tile becomes a single mountain tile surrounded by plains.
        tiles[2, 2].Terrain = TerrainType.Mountain;
        MapBuilder.SmoothSingleTileIslands(tiles);

        Assert.Equal(TerrainType.Plain, tiles[2, 2].Terrain);
    }

    private sealed class MapSpecStub : IMapSpec
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyDictionary<TerrainType, double> TileDistribution { get; }

        public MapSpecStub(
            int width,
            int height,
            IReadOnlyDictionary<TerrainType, double> tileDistribution)
        {
            Width = width;
            Height = height;
            TileDistribution = tileDistribution;
        }
    }
}
