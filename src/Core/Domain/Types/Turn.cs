namespace Core.Domain.Types;

/// <summary>
/// Represents the current turn index and active team.
/// Immutable value stored inside game state.
/// </summary>
public readonly struct Turn
{
    public int TurnNumber { get; }
    public TeamId TeamToAct { get; }

    public Turn(int turnNumber, TeamId teamToAct)
    {
        TurnNumber = turnNumber;
        TeamToAct = teamToAct;
    }

    public override string ToString() => $"Turn {TurnNumber} ({TeamToAct})";
}
