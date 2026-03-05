namespace Core.Tests.Map;

using Core.Domain.Types;
using Core.Map.Grid;
using Core.Map.Pathfinding;
using Core.Map.Search;
using Core.Map.Terrain;

public class PathfinderTests
{
    [Fact]
    public void GetReachable_Excludes_NonWalkable_And_Hole_Tiles()
    {
        var tiles = CreateTileArray(7, 7);
        var start = new HexCoord(3, 3);
        var east = MapSearch.GetNeighbourCoords(new Map(tiles), start).First();
        var northEast = MapSearch.GetNeighbourCoords(new Map(tiles), start).Skip(1).First();

        SetTerrain(tiles, east, TerrainType.Mountain);
        SetHole(tiles, northEast);

        var map = new Map(tiles);
        var pathfinder = new Pathfinder();

        var reachable = pathfinder.GetReachable(map, start, maxMoves: 2);

        Assert.False(reachable.ContainsKey(east));
        Assert.False(reachable.ContainsKey(northEast));
    }

    [Fact]
    public void IsMoveValid_Returns_True_For_Reachable_And_False_For_Unreachable()
    {
        var map = CreateMap(7, 7);
        var start = new HexCoord(3, 3);
        var destination = new HexCoord(4, 3);
        var far = new HexCoord(6, 3);
        var pathfinder = new Pathfinder();

        Assert.True(pathfinder.IsMoveValid(map, start, destination, maxMoves: 1));
        Assert.False(pathfinder.IsMoveValid(map, start, far, maxMoves: 1));
    }

    [Fact]
    public void GetMoveCost_Returns_Null_For_Unreachable_And_Start_Invalid()
    {
        var tiles = CreateTileArray(5, 5);
        var start = new HexCoord(2, 2);
        var destination = new HexCoord(3, 2);
        SetTerrain(tiles, destination, TerrainType.Mountain);
        var map = new Map(tiles);
        var pathfinder = new Pathfinder();

        var unreachableCost = pathfinder.GetMoveCost(map, start, destination);

        SetTerrain(tiles, start, TerrainType.Water);
        var blockedStartMap = new Map(tiles);
        var blockedStartCost = pathfinder.GetMoveCost(blockedStartMap, start, new HexCoord(2, 1));

        Assert.Null(unreachableCost);
        Assert.Null(blockedStartCost);
    }

    [Fact]
    public void GetMoveCost_Returns_Minimum_Path_Cost_For_Reachable_Destination()
    {
        var map = CreateMap(7, 7);
        var start = new HexCoord(1, 1);
        var destination = new HexCoord(3, 1);
        var pathfinder = new Pathfinder();

        var cost = pathfinder.GetMoveCost(map, start, destination);

        Assert.Equal(2, cost);
    }

    [Fact]
    public void HasLineOfSight_Returns_False_If_Target_Is_Mountain()
    {
        var tiles = CreateTileArray(7, 7);
        var from = new HexCoord(1, 1);
        var to = new HexCoord(3, 1);
        SetTerrain(tiles, to, TerrainType.Mountain);
        var map = new Map(tiles);
        var pathfinder = new Pathfinder();

        var hasLos = pathfinder.HasLineOfSight(map, from, to);

        Assert.False(hasLos);
    }

    [Fact]
    public void HasLineOfSight_Returns_False_When_Intermediate_Mountain_Blocks_Path()
    {
        var tiles = CreateTileArray(7, 7);
        var from = new HexCoord(1, 1);
        var to = new HexCoord(4, 1);
        var blocker = new HexCoord(2, 1);
        SetTerrain(tiles, blocker, TerrainType.Mountain);
        var map = new Map(tiles);
        var pathfinder = new Pathfinder();

        var hasLos = pathfinder.HasLineOfSight(map, from, to);

        Assert.False(hasLos);
    }

    [Fact]
    public void HasLineOfSight_Returns_True_For_Clear_Path_And_Same_Tile()
    {
        var map = CreateMap(7, 7);
        var from = new HexCoord(2, 2);
        var to = new HexCoord(4, 2);
        var pathfinder = new Pathfinder();

        Assert.True(pathfinder.HasLineOfSight(map, from, to));
        Assert.True(pathfinder.HasLineOfSight(map, from, from));
    }

    [Fact]
    public void HasLineOfSight_Returns_False_When_From_Or_To_Is_Missing()
    {
        var tiles = CreateTileArray(5, 5);
        var from = new HexCoord(1, 1);
        var to = new HexCoord(3, 1);
        SetHole(tiles, from);
        var mapMissingFrom = new Map(tiles);

        var tiles2 = CreateTileArray(5, 5);
        SetHole(tiles2, to);
        var mapMissingTo = new Map(tiles2);
        var pathfinder = new Pathfinder();

        Assert.False(pathfinder.HasLineOfSight(mapMissingFrom, from, to));
        Assert.False(pathfinder.HasLineOfSight(mapMissingTo, from, to));
    }

    private static Map CreateMap(int width, int height) => new(CreateTileArray(width, height));

    private static Tile[,] CreateTileArray(int width, int height)
    {
        var tiles = new Tile[width, height];
        for (var col = 0; col < width; col++)
            for (var row = 0; row < height; row++)
                tiles[col, row] = new Tile();
        return tiles;
    }

    private static void SetTerrain(Tile[,] tiles, HexCoord coord, TerrainType terrain)
    {
        var (col, row) = HexCoordConverter.ToOffset(coord);
        tiles[col, row].Terrain = terrain;
    }

    private static void SetHole(Tile[,] tiles, HexCoord coord)
    {
        var (col, row) = HexCoordConverter.ToOffset(coord);
        tiles[col, row] = null!;
    }
}
