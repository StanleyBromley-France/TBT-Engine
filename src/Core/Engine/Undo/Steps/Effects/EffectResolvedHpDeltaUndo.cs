namespace Core.Engine.Undo.Steps.Effects;

using System;
using System.Linq;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Types;
using Core.Game;

public sealed class EffectResolvedHpDeltaUndo : IUndoStep
{
    public UnitInstanceId TargetUnitId { get; }
    public EffectInstanceId EffectId { get; }
    public EffectComponentInstanceId ComponentId { get; }
    public int? OldResolvedHpDelta { get; }

    public EffectResolvedHpDeltaUndo(
        UnitInstanceId targetUnitId,
        EffectInstanceId effectId,
        EffectComponentInstanceId componentId,
        int? oldResolvedHpDelta)
    {
        TargetUnitId = targetUnitId;
        EffectId = effectId;
        ComponentId = componentId;
        OldResolvedHpDelta = oldResolvedHpDelta;
    }

    public void Undo(GameState state)
    {
        var component = state.ActiveEffects[TargetUnitId][EffectId]
            .Components
            .Single(c => c.Id == ComponentId);

        if (component is not IResolvableHpDeltaComponent resolvable)
        {
            throw new InvalidOperationException(
                $"Component '{ComponentId}' does not implement {nameof(IResolvableHpDeltaComponent)}.");
        }

        resolvable.ResolvedHpDelta = OldResolvedHpDelta;
    }
}