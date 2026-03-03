namespace Core.Engine.Undo.Steps.Effects;

using Core.Domain.Types;
using Core.Engine.Undo;
using Core.Game;

public sealed class EffectTickStateUndo : IUndoStep
{
    public UnitInstanceId TargetUnitId { get; }
    public EffectInstanceId EffectId { get; }
    public int OldRemainingTicks { get; }

    public EffectTickStateUndo(
        UnitInstanceId targetUnitId,
        EffectInstanceId effectId,
        int oldRemainingTicks)
    {
        TargetUnitId = targetUnitId;
        EffectId = effectId;
        OldRemainingTicks = oldRemainingTicks;
    }

    public void Undo(GameState state)
    {
        var effect = state.ActiveEffects[TargetUnitId][EffectId];
        effect.RemainingTicks = OldRemainingTicks;
    }
}