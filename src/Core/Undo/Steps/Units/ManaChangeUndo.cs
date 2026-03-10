namespace Core.Undo.Steps.Units;

using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

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