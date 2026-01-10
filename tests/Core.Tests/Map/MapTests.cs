using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Tests.Map
{
    using Core.Map.Algorithms;
    using Core.Map.Grid;

    public class HexCoordConverterTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(-1, 0)]
        [InlineData(2, -3)]
        public void ToOffset_FromOffset_RoundTrips_Axial(int q, int r)
        {
            var axial = new HexCoord(q, r);

            var (col, row) = HexCoordConverter.ToOffset(axial);
            var roundTripped = HexCoordConverter.FromOffset(col, row);

            Assert.Equal(axial, roundTripped);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(1, 2)]
        [InlineData(-1, 3)]
        [InlineData(4, -2)]
        public void FromOffset_ToOffset_RoundTrips_Offset(int col, int row)
        {
            var axial = HexCoordConverter.FromOffset(col, row);
            var (roundCol, roundRow) = HexCoordConverter.ToOffset(axial);

            Assert.Equal(col, roundCol);
            Assert.Equal(row, roundRow);
        }
    }

    public class HexCoordTests
    {
        [Fact]
        public void Equality_Compares_By_Value()
        {
            var a = new HexCoord(1, 2);
            var b = new HexCoord(1, 2);
            var c = new HexCoord(2, 1);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
            Assert.False(a.Equals(c));
        }

        [Fact]
        public void GetHashCode_Equal_For_Equal_Values()
        {
            var a = new HexCoord(3, -1);
            var b = new HexCoord(3, -1);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Operators_Add_And_Subtract_Work()
        {
            var a = new HexCoord(1, 2);
            var b = new HexCoord(3, -1);

            var sum = a + b;
            var diff = a - b;

            Assert.Equal(new HexCoord(4, 1), sum);
            Assert.Equal(new HexCoord(-2, 3), diff);
        }

        [Fact]
        public void ToString_Formats_As_QR_Tuple()
        {
            var coord = new HexCoord(2, -3);

            Assert.Equal("(2,-3)", coord.ToString());
        }
    }

    public class HexMathTests
    {
        [Theory]
        [InlineData(HexDirection.East, 1, 0)]
        [InlineData(HexDirection.NorthEast, 1, -1)]
        [InlineData(HexDirection.NorthWest, 0, -1)]
        [InlineData(HexDirection.West, -1, 0)]
        [InlineData(HexDirection.SouthWest, -1, 1)]
        [InlineData(HexDirection.SouthEast, 0, 1)]
        public void GetCoordInDirection_From_Origin_Uses_Correct_Deltas(
            HexDirection direction, int expectedQ, int expectedR)
        {
            var origin = new HexCoord(0, 0);

            var result = HexMath.GetCoordInDirection(origin, direction);

            Assert.Equal(new HexCoord(expectedQ, expectedR), result);
        }

        [Fact]
        public void GetCoordInDirection_Uses_Distance_Multiplier()
        {
            var origin = new HexCoord(0, 0);

            var result = HexMath.GetCoordInDirection(origin, HexDirection.East, 3);

            Assert.Equal(new HexCoord(3, 0), result);
        }

        [Fact]
        public void GetNeighborCoords_Returns_Six_Distinct_Neighbors()
        {
            var center = new HexCoord(0, 0);

            var neighbors = HexMath.GetNeighborCoords(center).ToArray();

            Assert.Equal(6, neighbors.Length);
            Assert.Equal(neighbors.Distinct().Count(), neighbors.Length);

            // All neighbors should be at distance 1
            foreach (var n in neighbors)
                Assert.Equal(1, HexMath.GetDistance(center, n));
        }

        [Fact]
        public void GetDistance_Zero_When_Same_Coord()
        {
            var a = new HexCoord(2, -1);

            var distance = HexMath.GetDistance(a, a);

            Assert.Equal(0, distance);
        }

        [Theory]
        [InlineData(0, 0, 1, 0, 1)]
        [InlineData(0, 0, 1, -1, 1)]
        [InlineData(0, 0, 0, 2, 2)]
        [InlineData(0, 0, -2, 2, 2)]
        [InlineData(1, -2, 3, -3, 2)]
        public void GetDistance_Produces_Known_Values(
            int aq, int ar, int bq, int br, int expected)
        {
            var a = new HexCoord(aq, ar);
            var b = new HexCoord(bq, br);

            var distance = HexMath.GetDistance(a, b);

            Assert.Equal(expected, distance);
        }

        [Fact]
        public void GetCoordsInCircle_Radius_Zero_Returns_Only_Center()
        {
            var center = new HexCoord(0, 0);

            var result = HexMath.GetCoordsInRadius(center, 0).ToArray();

            Assert.Single(result);
            Assert.Equal(center, result[0]);
        }

        [Fact]
        public void GetCoordsInCircle_Radius_One_Returns_Center_And_Neighbors()
        {
            var center = new HexCoord(0, 0);

            var result = HexMath.GetCoordsInRadius(center, 1).ToArray();

            Assert.Equal(7, result.Length); // 1 center + 6 neighbors

            Assert.Contains(center, result);
            var neighbors = HexMath.GetNeighborCoords(center).ToArray();

            foreach (var neighbor in neighbors)
                Assert.Contains(neighbor, result);
        }

        [Fact]
        public void GetCoordsInCircle_Radius_Two_Returns_Correct_Count()
        {
            var center = new HexCoord(0, 0);

            var result = HexMath.GetCoordsInRadius(center, 2).ToArray();

            // Known formula: 1 + 3r(r + 1) for radius r
            const int expectedCount = 19;

            Assert.Equal(expectedCount, result.Length);

            // All coords should be within distance 2
            foreach (var coord in result)
                Assert.True(HexMath.GetDistance(center, coord) <= 2);
        }
    }

    public class TerrainRulesTests
    {
        [Theory]
        [InlineData(TerrainType.Plain, true)]
        [InlineData(TerrainType.Forest, true)]
        [InlineData(TerrainType.Mountain, false)]
        [InlineData(TerrainType.Water, false)]
        public void IsWalkable_Depends_On_Terrain(TerrainType terrain, bool expected)
        {
            var result = TerrainRules.IsWalkable(terrain);

            Assert.Equal(expected, result);
        }
    }

    public class TileTests
    {
        [Theory]
        [InlineData(TerrainType.Plain, true)]
        [InlineData(TerrainType.Forest, true)]
        [InlineData(TerrainType.Mountain, false)]
        [InlineData(TerrainType.Water, false)]
        public void IsWalkable_Delegates_To_TerrainRules(TerrainType terrain, bool expected)
        {
            var tile = new Tile { Terrain = terrain };

            Assert.Equal(expected, tile.IsWalkable);
        }
    }

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
    }
}
