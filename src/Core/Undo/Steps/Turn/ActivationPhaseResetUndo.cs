namespace Core.Undo.Steps.Turn;

using System;
using System.Collections.Generic;
using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

public sealed class ActivationPhaseResetUndo : IUndoStep
{
    public UnitInstanceId OldActiveUnitId { get; }
    public UnitInstanceId? OldCurrentlyCommiting { get; }
    public IReadOnlyList<UnitInstanceId> OldCommittedUnits { get; }

    public ActivationPhaseResetUndo(
        UnitInstanceId oldActiveUnitId,
        UnitInstanceId? oldCurrentlyCommiting,
        IReadOnlyList<UnitInstanceId> oldCommittedUnits)
    {
        OldActiveUnitId = oldActiveUnitId;
        OldCurrentlyCommiting = oldCurrentlyCommiting;
        OldCommittedUnits = oldCommittedUnits ?? throw new ArgumentNullException(nameof(oldCommittedUnits));
    }

    public void Undo(GameState state)
    {
        state.Phase.ActiveUnitId = OldActiveUnitId;
        state.Phase.CurrentlyCommiting = OldCurrentlyCommiting;

        state.Phase.CommittedThisPhase.Clear();
        foreach (var id in OldCommittedUnits)
            state.Phase.CommittedThisPhase.Add(id);
    }
}
