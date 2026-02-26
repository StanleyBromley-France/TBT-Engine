namespace Core.Engine.Undo.Steps;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

public sealed class ManaChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public int OldMana { get; }

    public ManaChangeUndo(UnitInstanceId unitId, int oldMana)
    {
        UnitId = unitId;
        OldMana = oldMana;
    }

    public void Undo(GameState state)
    {
        state.UnitInstances[UnitId].Resources.Mana = OldMana;
    }
}