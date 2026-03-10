namespace Core.Undo.Steps.Move;

using Core.Domain.Types;
using Core.Game.State;
using Core.Undo.Steps;

public sealed class UnitPositionChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public HexCoord OldPosition { get; }

    public UnitPositionChangeUndo(UnitInstanceId unitId, HexCoord oldPosition)
    {
        UnitId = unitId;
        OldPosition = oldPosition;
    }

    public void Undo(GameState state)
    {
        var unit = state.UnitInstances[UnitId];

        state.OccupiedHexes.Remove(unit.Position);
        state.OccupiedHexes.Add(OldPosition);
        state.UnitInstances[UnitId].Position = OldPosition;
    }
}