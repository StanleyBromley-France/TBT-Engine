namespace Core.Domain.Types;

/// <summary>
/// Represents the current turn index and active team.
/// Immutable value stored inside game state.
/// </summary>
public readonly struct Turn
{
    public int AttackerTurnsTaken { get; }
    public TeamId TeamToAct { get; }

    public Turn(int attackerTurnsTaken, TeamId teamToAct)
    {
        AttackerTurnsTaken = attackerTurnsTaken;
        TeamToAct = teamToAct;
    }
}
