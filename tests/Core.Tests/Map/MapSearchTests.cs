namespace Core.Tests.Map;

using Core.Domain.Types;
using Core.Map.Grid;
using Core.Map.Search;
using Core.Map.Terrain;

public sealed class MapSearchTests
{
    private static Tile[,] CreateTileArray(int width, int height)
    {
        var tiles = new Tile[width, height];

        for (var col = 0; col < width; col++)
            for (var row = 0; row < height; row++)
                tiles[col, row] = new Tile();

        return tiles;
    }

    private static Map CreateMap(int width, int height)
        => new(CreateTileArray(width, height));

    [Fact]
    public void GetNeighbourCoords_Interior_ReturnsSix()
    {
        var map = CreateMap(5, 5);
        var center = new HexCoord(2, 2);

        var result = MapSearch.GetNeighbourCoords(map, center).ToList();
        var expected = new HashSet<HexCoord>
        {
            new(3, 2),  // East
            new(3, 1),  // NorthEast
            new(2, 1),  // NorthWest
            new(1, 2),  // West
            new(1, 3),  // SouthWest
            new(2, 3)   // SouthEast
        };

        Assert.Equal(6, result.Count);
        Assert.Equal(expected, result.ToHashSet());
        Assert.All(result, c => Assert.True(map.TryGetTile(c, out _)));
    }

    [Fact]
    public void GetNeighbourCoords_Corner_TruncatesCorrectly()
    {
        var map = CreateMap(5, 5);
        var corner = new HexCoord(0, 0);

        var result = MapSearch.GetNeighbourCoords(map, corner).ToList();

        Assert.True(result.Count > 0);
        Assert.True(result.Count < 6);
        Assert.All(result, c => Assert.True(map.TryGetTile(c, out _)));
    }

    [Fact]
    public void GetNeighbourCoords_Excludes_Hole()
    {
        var tiles = CreateTileArray(5, 5);
        var center = new HexCoord(2, 2);

        var mapForNeighbourCalc = new Map(tiles);

        var east = MapSearch
            .GetNeighbourCoords(mapForNeighbourCalc, center)
            .First();

        var (col, row) = HexCoordConverter.ToOffset(east);
        tiles[col, row] = null!;

        var map = new Map(tiles);

        var neighbours = MapSearch.GetNeighbourCoords(map, center).ToList();

        Assert.DoesNotContain(east, neighbours);
        Assert.All(neighbours, c => Assert.True(map.TryGetTile(c, out _)));
    }

    [Fact]
    public void GetCoordsInRadius_ZeroRadius_ReturnsCenter()
    {
        var map = CreateMap(5, 5);
        var center = new HexCoord(2, 2);

        var result = MapSearch.GetCoordsInRadius(map, center, 0).ToList();

        Assert.Single(result);
        Assert.Equal(center, result[0]);
    }

    [Fact]
    public void GetCoordsInRadius_InteriorRadius_ReturnsExactCount()
    {
        var map = CreateMap(7, 7);
        var center = new HexCoord(3, 3);
        const int radius = 2;

        var result = MapSearch.GetCoordsInRadius(map, center, radius).ToList();

        Assert.Equal(1 + 3 * radius * (radius + 1), result.Count);
    }

    [Fact]
    public void GetCoordsInRadius_Radius1_Returns_Exact_Hex_Set()
    {
        var map = CreateMap(7, 7);
        var center = new HexCoord(3, 3);

        var result = MapSearch.GetCoordsInRadius(map, center, 1).ToHashSet();
        var expected = new HashSet<HexCoord>
        {
            new(3, 3),
            new(4, 3),
            new(4, 2),
            new(3, 2),
            new(2, 3),
            new(2, 4),
            new(3, 4)
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetCoordsInRadius_EdgeRadius_ClipsToMap()
    {
        var map = CreateMap(5, 5);
        var edge = new HexCoord(0, 0);

        var result = MapSearch.GetCoordsInRadius(map, edge, 3).ToList();

        Assert.NotEmpty(result);
        Assert.All(result, c => Assert.True(map.TryGetTile(c, out _)));
    }

    [Fact]
    public void GetCoordsInRay_Interior_ReturnsExactDistance()
    {
        var map = CreateMap(7, 7);
        var start = new HexCoord(2, 2);

        var result = MapSearch
            .GetCoordsInRay(map, start, HexDirection.East, 3)
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { new HexCoord(3, 2), new HexCoord(4, 2), new HexCoord(5, 2) }, result);
    }

    [Fact]
    public void GetCoordsInRay_StopsAtMapEdge()
    {
        var map = CreateMap(3, 3);
        var start = new HexCoord(1, 1);

        var result = MapSearch
            .GetCoordsInRay(map, start, HexDirection.West, 10)
            .ToList();

        Assert.True(result.Count < 10);
        Assert.All(result, c => Assert.True(map.TryGetTile(c, out _)));
    }

    [Fact]
    public void GetCoordsInRay_Stops_When_Hitting_Hole()
    {
        var tiles = CreateTileArray(10, 10);
        var start = new HexCoord(2, 2);
        var step1 = new HexCoord(3, 2); // first step East from (2,2)
        var step2 = new HexCoord(4, 2); // second step East from (2,2)

        // Create hole at step2
        var (col, row) = HexCoordConverter.ToOffset(step2);
        tiles[col, row] = null!;

        var map = new Map(tiles);

        var ray = MapSearch.GetCoordsInRay(map, start, HexDirection.East, 5).ToList();

        Assert.Contains(step1, ray);
        Assert.DoesNotContain(step2, ray);
        Assert.Single(ray);
    }

    [Fact]
    public void GetTilesInRay_StopsAtMapEdge()
    {
        var map = CreateMap(3, 3);
        var start = new HexCoord(1, 1);

        var tiles = MapSearch
            .GetTilesInRay(map, start, HexDirection.NorthWest, 10)
            .ToList();

        Assert.NotEmpty(tiles);
        Assert.All(tiles, t => Assert.NotNull(t));
    }
}
