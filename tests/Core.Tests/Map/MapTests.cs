namespace Core.Tests.Map;

using Core.Map.Algorithms;
using Core.Map.Grid;
using Core.Types;
public class MapTests
{
    [Fact]
    public void Constructor_Initializes_Tiles_With_Plain_Terrain()
    {
        var map = new Map(3, 2);

        Assert.Equal(3, map.Width);
        Assert.Equal(2, map.Height);

        for (int col = 0; col < map.Width; col++)
        {
            for (int row = 0; row < map.Height; row++)
            {
                Assert.NotNull(map.Tiles[col, row]);
                Assert.Equal(TerrainType.Plain, map.Tiles[col, row].Terrain);
            }
        }
    }

    [Fact]
    public void IsInside_True_For_Bounds_False_Otherwise()
    {
        var map = new Map(3, 2);

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
        var map = new Map(5, 5);
        var axial = new HexCoord(2, 1);

        var (col, row) = HexCoordConverter.ToOffset(axial);

        // Ensure this axial actually maps inside the map
        Assert.True(map.IsInside(col, row));

        var tileFromMap = map.GetTile(axial);

        Assert.NotNull(tileFromMap);
        Assert.Same(map.Tiles[col, row], tileFromMap);
    }

    [Fact]
    public void GetTile_Returns_Null_For_Out_Of_Bounds_Axial_Coord()
    {
        var map = new Map(3, 3);
        var axialOutside = new HexCoord(5, 0); // col = 5, clearly outside width 3

        var tile = map.GetTile(axialOutside);

        Assert.Null(tile);
    }

    [Fact]
    public void GetTile_Returns_Null_For_Hole_InsideBounds()
    {
        var map = new Map(3, 3);

        var (col, row) = (1, 1);
        map.Tiles[col, row] = null!;
        var hole = HexCoordConverter.FromOffset(col, row);

        Assert.Null(map.GetTile(hole));
    }


    [Fact]
    public void TryGetTile_Returns_False_And_Null_For_OutOfBounds()
    {
        var map = new Map(2, 2);

        var result = map.TryGetTile(new HexCoord(10, 10), out var tile);

        Assert.False(result);
        Assert.Null(tile);
    }

    [Fact]
    public void TryGetTile_Returns_True_And_Tile_For_Valid_Coord()
    {
        var map = new Map(3, 3);
        var coord = new HexCoord(1, 1);

        var result = map.TryGetTile(coord, out var tile);

        Assert.True(result);
        Assert.NotNull(tile);
    }

    [Fact]
    public void TryGetTile_Returns_False_For_Hole_InsideBounds()
    {
        var map = new Map(3, 3);

        var (col, row) = (1, 1);
        map.Tiles[col, row] = null!;
        var hole = HexCoordConverter.FromOffset(col, row);

        var ok = map.TryGetTile(hole, out var tile);

        Assert.False(ok);
        Assert.Null(tile);
    }

}

