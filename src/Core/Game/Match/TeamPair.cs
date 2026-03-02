namespace Core.Game.Match;

using Core.Domain.Types;

public sealed class TeamPair
{
    public TeamId TeamA { get; }
    public TeamId TeamB { get; }

    public TeamPair(TeamId teamA, TeamId teamB)
    {
        if (teamA.Equals(teamB))
            throw new ArgumentException("Teams must be different.");

        TeamA = teamA;
        TeamB = teamB;
    }

    public TeamId GetOpposingTeam(TeamId team)
    {
        if (team.Equals(TeamA)) return TeamB;
        if (team.Equals(TeamB)) return TeamA;

        throw new InvalidOperationException("Unknown team.");
    }
}