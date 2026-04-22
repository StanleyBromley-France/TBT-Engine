namespace Core.Tests.Random;

using Core.Random;

public sealed class SeedDeriverTests
{
    [Fact]
    public void Derive_SameSeedAndSalt_ReturnsSameValue()
    {
        var first = SeedDeriver.Derive(12345, 17);
        var second = SeedDeriver.Derive(12345, 17);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Derive_DifferentSalts_ReturnDifferentValues()
    {
        var first = SeedDeriver.Derive(12345, 17);
        var second = SeedDeriver.Derive(12345, 23);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Derive_DifferentSeeds_ReturnDifferentValues()
    {
        var first = SeedDeriver.Derive(12345, 17);
        var second = SeedDeriver.Derive(54321, 17);

        Assert.NotEqual(first, second);
    }
}
