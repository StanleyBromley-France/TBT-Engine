namespace Core.Tests.Map;

using Core.Map.Algorithms;
using Core.Map.Grid;
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
    public void GetCoordsInRay_Produces_Contiguous_Steps()
    {
        var start = new HexCoord(0, 0);

        var ray = HexMath.GetCoordsInRay(start, HexDirection.East, 3).ToArray();

        Assert.Equal(
            new[]
            {
            new HexCoord(1, 0),
            new HexCoord(2, 0),
            new HexCoord(3, 0)
            },
            ray
        );
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

