namespace Core.Undo.Steps.Units;

using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

public sealed class HpChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public int OldHp { get; }
    public HexCoord AffectedHex { get; }
    public bool WasHexOccupiedBeforeChange { get; }

    public HpChangeUndo(
        UnitInstanceId unitId,
        int oldHp,
        HexCoord affectedHex,
        bool wasHexOccupiedBeforeChange)
    {
        UnitId = unitId;
        OldHp = oldHp;
        AffectedHex = affectedHex;
        WasHexOccupiedBeforeChange = wasHexOccupiedBeforeChange;
    }

    public void Undo(GameState state)
    {
        var unit = state.UnitInstances[UnitId];
        unit.Resources.HP = OldHp;

        if (WasHexOccupiedBeforeChange)
            state.OccupiedHexes.Add(AffectedHex);
        else
            state.OccupiedHexes.Remove(AffectedHex);
    }
}
