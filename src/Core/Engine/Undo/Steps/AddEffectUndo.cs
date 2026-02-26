namespace Core.Engine.Undo.Steps;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

public sealed class AddEffectUndo : IUndoStep
{
    public UnitInstanceId TargetUnitId { get; }
    public EffectInstanceId EffectId { get; }

    public AddEffectUndo(UnitInstanceId targetUnitId, EffectInstanceId effectId)
    {
        TargetUnitId = targetUnitId;
        EffectId = effectId;
    }

    public void Undo(GameState state)
    {
        if (state.ActiveEffects.TryGetValue(TargetUnitId, out var effects))
            effects.Remove(EffectId);
    }
}