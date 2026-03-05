namespace Core.Tests.Map;

using Core.Domain.Types;
using Core.Map.Grid;
using Core.Map.Search;
using Core.Map.Terrain;

public sealed class MapTests
{
    private static Tile[,] CreateTileArray(int width, int height)
    {
        var tiles = new Tile[width, height];
        for (var col = 0; col < width; col++)
            for (var row = 0; row < height; row++)
                tiles[col, row] = new Tile(); // assumes default terrain is Plain
        return tiles;
    }

    [Fact]
    public void Constructor_Initializes_Width_Height_From_Array()
    {
        var tiles = CreateTileArray(3, 2);

        var map = new Map(tiles);

        Assert.Equal(3, map.Width);
        Assert.Equal(2, map.Height);
    }

    [Fact]
    public void Constructor_Uses_Provided_Tiles()
    {
        var tiles = CreateTileArray(3, 2);

        // spot-check: ensure we can retrieve a known tile instance through axial coord mapping
        var (col, row) = (1, 0);
        var expected = tiles[col, row];
        var coord = HexCoordConverter.FromOffset(col, row);

        var map = new Map(tiles);

        var actual = map.GetTile(coord);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void IsInside_True_For_Bounds_False_Otherwise()
    {
        var map = new Map(CreateTileArray(3, 2));

        Assert.True(map.IsInside(0, 0));
        Assert.True(map.IsInside(2, 1));

        Assert.False(map.IsInside(-1, 0));
        Assert.False(map.IsInside(0, -1));
        Assert.False(map.IsInside(3, 0));
        Assert.False(map.IsInside(0, 2));
    }

    [Fact]
    public void GetTile_Returns_Tile_For_Valid_Axial_Coord()
    {
        var tiles = CreateTileArray(5, 5);
        var map = new Map(tiles);

        var axial = new HexCoord(2, 1);
        var (col, row) = HexCoordConverter.ToOffset(axial);

        Assert.True(map.IsInside(col, row));

        var tileFromMap = map.GetTile(axial);

        Assert.NotNull(tileFromMap);
        Assert.Same(tiles[col, row], tileFromMap);
    }

    [Fact]
    public void GetTile_Returns_Null_For_Out_Of_Bounds_Axial_Coord()
    {
        var map = new Map(CreateTileArray(3, 3));
        var axialOutside = new HexCoord(5, 0);

        var tile = map.GetTile(axialOutside);

        Assert.Null(tile);
    }

    [Fact]
    public void GetTile_Returns_Null_For_Hole_InsideBounds()
    {
        var tiles = CreateTileArray(3, 3);

        var (col, row) = (1, 1);
        tiles[col, row] = null!; // create hole before constructing map

        var map = new Map(tiles);
        var hole = HexCoordConverter.FromOffset(col, row);

        Assert.Null(map.GetTile(hole));
    }

    [Fact]
    public void TryGetTile_Returns_False_And_Null_For_OutOfBounds()
    {
        var map = new Map(CreateTileArray(2, 2));

        var ok = map.TryGetTile(new HexCoord(10, 10), out var tile);

        Assert.False(ok);
        Assert.Null(tile);
    }

    [Fact]
    public void TryGetTile_Returns_True_And_Tile_For_Valid_Coord()
    {
        var map = new Map(CreateTileArray(3, 3));
        var coord = new HexCoord(1, 1);

        var ok = map.TryGetTile(coord, out var tile);

        Assert.True(ok);
        Assert.NotNull(tile);
    }

    [Fact]
    public void TryGetTile_Returns_False_For_Hole_InsideBounds()
    {
        var tiles = CreateTileArray(3, 3);

        var (col, row) = (1, 1);
        tiles[col, row] = null!;

        var map = new Map(tiles);
        var hole = HexCoordConverter.FromOffset(col, row);

        var ok = map.TryGetTile(hole, out var tile);

        Assert.False(ok);
        Assert.Null(tile);
    }
}