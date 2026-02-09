namespace Core.Game;

using Core.Domain.Types;

/// <summary>
/// Represents the active team and turn index for the current moment in the match.
/// This is an immutable value used as part of <see cref="GameState"/> to track
/// structured turn progression.
/// </summary>
public sealed class Turn
{
    public int TurnNumber { get; }
    public TeamId TeamToAct { get; }

    public Turn(int turnNumber, TeamId teamToAct)
    {
        TurnNumber = turnNumber;
        TeamToAct = teamToAct;
    }

    /// <summary>
    /// Produces a new turn value with an incremented turn number and a new active team.
    /// </summary>
    public Turn Next(TeamId nextTeam) => new Turn(TurnNumber + 1, nextTeam);
}

