namespace Core.Game.Match;

using Core.Domain.Types;

public enum GameOutcomeType
{
    Ongoing,
    Victory,
    Draw
}

public sealed class GameOutcome
{
    public GameOutcomeType Type { get; }
    public TeamId? WinningTeam { get; }

    private GameOutcome(GameOutcomeType type, TeamId? winningTeam)
    {
        Type = type;
        WinningTeam = winningTeam;
    }

    public static GameOutcome Ongoing() =>
        new(GameOutcomeType.Ongoing, null);

    public static GameOutcome Victory(TeamId winner) =>
        new(GameOutcomeType.Victory, winner);

    public static GameOutcome Draw() =>
        new(GameOutcomeType.Draw, null);
}