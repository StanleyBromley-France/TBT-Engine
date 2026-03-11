namespace Core.Undo.Steps.Units;

using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

public sealed class HpChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public int OldHp { get; }
    public bool WasAliveBeforeChange { get; }

    public HpChangeUndo(UnitInstanceId unitId, int oldHp, bool wasAliveBeforeChange)
    {
        UnitId = unitId;
        OldHp = oldHp;
        WasAliveBeforeChange = wasAliveBeforeChange;
    }

    public void Undo(GameState state)
    {
        var unit = state.UnitInstances[UnitId];
        unit.Resources.HP = OldHp;

        if (WasAliveBeforeChange)
            state.OccupiedHexes.Add(unit.Position);
        else
            state.OccupiedHexes.Remove(unit.Position);
    }
}
