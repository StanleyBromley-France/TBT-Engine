namespace Core.Tests.Map;

using Core.Types;

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

