namespace Core.Game.Match;

using Core.Domain.Types;

public sealed class TeamPair
{
    public TeamId Attacker { get; }
    public TeamId Defender { get; }

    public TeamPair(TeamId attacker, TeamId defender)
    {
        if (attacker.Equals(defender))
            throw new ArgumentException("Teams must be different.");

        Attacker = attacker;
        Defender = defender;
    }

    public bool IsAttacker(TeamId team) => team.Equals(Attacker);
    public bool IsDefender(TeamId team) => team.Equals(Defender);

    public TeamId GetOpposingTeam(TeamId team)
    {
        if (team.Equals(Attacker)) return Defender;
        if (team.Equals(Defender)) return Attacker;

        throw new InvalidOperationException("Unknown team.");
    }
}