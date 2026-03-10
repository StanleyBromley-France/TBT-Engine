namespace Core.Undo.Steps.Effects;

using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Types;
using Core.Game;
using Core.Undo.Steps;

public sealed class RemoveEffectUndo : IUndoStep
{
    public UnitInstanceId TargetUnitId { get; }
    public EffectInstance Effect { get; }

    public RemoveEffectUndo(UnitInstanceId targetUnitId, EffectInstance effect)
    {
        TargetUnitId = targetUnitId;
        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
    }

    public void Undo(GameState state)
    {
        if (!state.ActiveEffects.TryGetValue(TargetUnitId, out var effects))
        {
            effects = new Dictionary<EffectInstanceId, EffectInstance>();
            state.ActiveEffects[TargetUnitId] = effects;
        }

        effects.Add(Effect.Id, Effect);
    }
}