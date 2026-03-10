namespace Core.Undo.Steps.Turn;

using Core.Domain.Types;
using Core.Game;
using Core.Undo.Steps;

public sealed class PhaseCommitUnitUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }

    public PhaseCommitUnitUndo(UnitInstanceId unitId)
    {
        UnitId = unitId;
    }

    public void Undo(GameState state)
    {
        state.Phase.CommittedThisPhase.Remove(UnitId);
    }
}