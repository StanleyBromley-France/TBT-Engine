namespace Core.Engine.Undo.Steps.Units;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

public sealed class HpChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public int OldHp { get; }

    public HpChangeUndo(UnitInstanceId unitId, int oldHp)
    {
        UnitId = unitId;
        OldHp = oldHp;
    }

    public void Undo(GameState state)
    {
        state.UnitInstances[UnitId].Resources.HP = OldHp;
    }
}