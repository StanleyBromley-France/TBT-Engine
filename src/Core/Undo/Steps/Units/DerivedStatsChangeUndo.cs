namespace Core.Undo.Steps.Units;

using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Game;
using Core.Undo.Steps;

public sealed class DerivedStatsChangeUndo : IUndoStep
{
    public UnitInstanceId UnitId { get; }
    public UnitDerivedStats OldStats { get; }

    public DerivedStatsChangeUndo(UnitInstanceId unitId, UnitDerivedStats oldStats)
    {
        UnitId = unitId;
        OldStats = oldStats;
    }

    public void Undo(GameState state)
    {
        state.UnitInstances[UnitId].DerivedStats = OldStats;
    }
}