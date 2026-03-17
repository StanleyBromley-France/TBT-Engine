namespace Core.Undo.Steps.Turn;

using System;
using System.Collections.Generic;
using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

public sealed class ActivationPhaseResetUndo : IUndoStep
{
    public UnitInstanceId? OldCurrentlyCommiting { get; }
    public IReadOnlyList<UnitInstanceId> OldCommittedUnits { get; }

    public ActivationPhaseResetUndo(
        UnitInstanceId? oldCurrentlyCommiting,
        IReadOnlyList<UnitInstanceId> oldCommittedUnits)
    {
        OldCurrentlyCommiting = oldCurrentlyCommiting;
        OldCommittedUnits = oldCommittedUnits ?? throw new ArgumentNullException(nameof(oldCommittedUnits));
    }

    public void Undo(GameState state)
    {
        state.Phase.CurrentlyCommiting = OldCurrentlyCommiting;

        state.Phase.CommittedThisPhase.Clear();
        foreach (var id in OldCommittedUnits)
            state.Phase.CommittedThisPhase.Add(id);
    }
}
