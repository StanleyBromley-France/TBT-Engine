namespace Core.Engine.Undo.Steps.Effects;

using Core.Domain.Types;
using Core.Game;

public sealed class EffectStackChangeUndo : IUndoStep
{
    public UnitInstanceId TargetUnitId { get; }
    public EffectInstanceId EffectId { get; }
    public int OldStacks { get; }

    public EffectStackChangeUndo(
        UnitInstanceId targetUnitId,
        EffectInstanceId effectId,
        int oldStacks)
    {
        TargetUnitId = targetUnitId;
        EffectId = effectId;
        OldStacks = oldStacks;
    }

    public void Undo(GameState state)
    {
        var effect = state.ActiveEffects[TargetUnitId][EffectId];
        effect.CurrentStacks = OldStacks;
    }
}