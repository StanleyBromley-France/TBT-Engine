namespace Core.Engine.Undo.Steps.Effects;

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
        if (!state.ActiveEffects.TryGetValue(TargetUnitId, out var effects))
            return;

        effects.Remove(EffectId);

        if (effects.Count == 0)
            state.ActiveEffects.Remove(TargetUnitId);
    }
}