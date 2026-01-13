namespace Core.Game;

using Core.Domain.Units;

/// <summary>
/// Represents the active team and turn index for the current moment in the match.
/// This is an immutable value used as part of <see cref="GameState"/> to track
/// structured turn progression.
/// </summary>
public sealed class Turn
{
    public int TurnNumber { get; }
    public Team TeamToAct { get; }

    public Turn(int turnNumber, Team teamToAct)
    {
        TurnNumber = turnNumber;
        TeamToAct = teamToAct;
    }

    /// <summary>
    /// Produces a new turn value with an incremented turn number and a new active team.
    /// </summary>
    public Turn Next(Team nextTeam) => new Turn(TurnNumber + 1, nextTeam);
}

