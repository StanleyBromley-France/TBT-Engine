using Core.Engine.Random;
using Core.Game;

namespace Core.Tests.Engine.Random;

public class DeterministicRngTests
{
    [Fact]
    public void Next_Returns_Deterministic_Value_And_Advanced_State()
    {
        var rng = new DeterministicRng();
        var state = new RngState(seed: 42, position: 7);

        var (value, nextState) = rng.Next(state);
        var expected = unchecked(42 * 1664525 + 1013904223 + 7);

        Assert.Equal(expected, value);
        Assert.Equal(42, nextState.Seed);
        Assert.Equal(8, nextState.Position);
    }

    [Fact]
    public void Same_State_Produces_Same_Output()
    {
        var rng = new DeterministicRng();
        var state = new RngState(seed: 9, position: 0);

        var first = rng.Next(state);
        var second = rng.Next(state);

        Assert.Equal(first.Value, second.Value);
        Assert.Equal(first.NextState.Position, second.NextState.Position);
    }

    [Fact]
    public void Sequential_Next_Uses_Updated_Position()
    {
        var rng = new DeterministicRng();
        var initial = new RngState(seed: 123, position: 0);

        var first = rng.Next(initial);
        var second = rng.Next(first.NextState);

        var expectedFirst = unchecked(123 * 1664525 + 1013904223 + 0);
        var expectedSecond = unchecked(123 * 1664525 + 1013904223 + 1);

        Assert.Equal(expectedFirst, first.Value);
        Assert.Equal(expectedSecond, second.Value);
    }
}
