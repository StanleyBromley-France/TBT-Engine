namespace Core.Game;

using Core.Domain.Types;

/// <summary>
/// Tracks per-team activation progress within a single turn phase.
/// 
/// A phase represents the period where one team is acting.
/// Units may act in any order, but each alive unit on the active team
/// must commit at least one action before the turn can end.
/// </summary>
public sealed class ActivationPhase
{
    /// <summary>
    /// The currently selected/active unit (for UI and action routing).
    /// </summary>
    public UnitInstanceId ActiveUnitId { get; set; }

    /// <summary>
    /// Units that have taken at least one committing action
    /// (Move, UseAbility, etc.) during this phase.
    /// </summary>
    public HashSet<UnitInstanceId> CommittedThisPhase { get; } = new();

    public ActivationPhase(UnitInstanceId initialActiveUnitId)
    {
        ActiveUnitId = initialActiveUnitId;
    }

    /// <summary>
    /// Marks a unit as having committed this phase.
    /// </summary>
    public void MarkCommitted(UnitInstanceId unitId)
    {
        CommittedThisPhase.Add(unitId);
    }

    /// <summary>
    /// Returns true if the unit has already committed this phase.
    /// </summary>
    public bool HasCommitted(UnitInstanceId unitId)
    {
        return CommittedThisPhase.Contains(unitId);
    }

    /// <summary>
    /// Clears phase progress (used when switching teams).
    /// </summary>
    public void Reset(UnitInstanceId newActiveUnitId)
    {
        CommittedThisPhase.Clear();
        ActiveUnitId = newActiveUnitId;
    }
}