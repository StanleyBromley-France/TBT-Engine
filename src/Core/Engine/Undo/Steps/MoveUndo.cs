namespace Core.Engine.Undo.Steps;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

public sealed class MoveUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public HexCoord OldPosition { get; }

    public MoveUndo(UnitInstanceId unitId, HexCoord oldPosition)
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