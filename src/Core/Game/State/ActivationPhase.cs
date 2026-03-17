namespace Core.Game.State;

using Core.Domain.Types;

/// <summary>
/// Tracks per-team activation progress within a single turn phase.
/// 
/// A phase represents the period where one team is acting.
/// Units may act in any order, but each alive unit on the active team
/// must fully spend its action points before the turn can end.
/// </summary>
public sealed class ActivationPhase
{
    /// <summary>
    /// The currently selected/active unit (for UI and action routing).
    /// </summary>
    public UnitInstanceId ActiveUnitId { get; set; }

    /// <summary>
    /// Units that have taken at least one committing action
    /// and finished acting for this phase by reaching 0 action points.
    /// </summary>
    public HashSet<UnitInstanceId> CommittedThisPhase { get; } = new();

    /// <summary>
    /// The unit currently spending action points this phase.
    /// Empty when no unit is mid-activation.
    /// </summary>
    public UnitInstanceId? CurrentlyCommiting { get; set; }

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

    public void SetCurrentlyCommiting(UnitInstanceId unitId)
    {
        CurrentlyCommiting = unitId;
    }

    public void ClearCurrentlyCommiting()
    {
        CurrentlyCommiting = null;
    }

    /// <summary>
    /// Returns true if the unit has already committed this phase.
    /// </summary>
    public bool HasCommitted(UnitInstanceId unitId)
    {
        return CommittedThisPhase.Contains(unitId);
    }

    public bool IsCurrentlyCommiting(UnitInstanceId unitId)
    {
        return CurrentlyCommiting.HasValue && CurrentlyCommiting.Value == unitId;
    }

    /// <summary>
    /// Clears phase progress (used when switching teams).
    /// </summary>
    public void Reset(UnitInstanceId newActiveUnitId)
    {
        CommittedThisPhase.Clear();
        CurrentlyCommiting = null;
        ActiveUnitId = newActiveUnitId;
    }

    public ActivationPhase DeepCloneForSimulation()
    {
        var clone = new ActivationPhase(ActiveUnitId);
        foreach (var unitId in CommittedThisPhase)
        {
            clone.CommittedThisPhase.Add(unitId);
        }

        clone.CurrentlyCommiting = CurrentlyCommiting;

        return clone;
    }
}
