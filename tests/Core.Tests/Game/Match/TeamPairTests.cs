using Core.Domain.Types;
using Core.Game.Match;

namespace Core.Tests.Game.Match;

public class TeamPairTests
{
    [Fact]
    public void IsAttacker_And_IsDefender_Return_Expected_Values()
    {
        var teams = new TeamPair(new TeamId(1), new TeamId(2));

        Assert.True(teams.IsAttacker(new TeamId(1)));
        Assert.False(teams.IsAttacker(new TeamId(2)));
        Assert.True(teams.IsDefender(new TeamId(2)));
        Assert.False(teams.IsDefender(new TeamId(1)));
    }

    [Fact]
    public void GetOpposingTeam_Returns_Other_Team()
    {
        var teams = new TeamPair(new TeamId(1), new TeamId(2));

        Assert.Equal(new TeamId(2), teams.GetOpposingTeam(new TeamId(1)));
        Assert.Equal(new TeamId(1), teams.GetOpposingTeam(new TeamId(2)));
    }
}
