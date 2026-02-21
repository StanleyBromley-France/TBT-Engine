namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.ReadOnly;

public sealed class DamageOverTimeComponentInstance : EffectComponentInstance<DamageOverTimeComponentTemplate>, IResolvableHpDeltaComponent
{
    private int? _resolvedDamagePerTick;

    int? IResolvableHpDeltaComponent.ResolvedHpDelta
    {
        get => _resolvedDamagePerTick;
        set => _resolvedDamagePerTick = value;
    }
    public DamageOverTimeComponentInstance(
        EffectComponentInstanceId id,
        DamageOverTimeComponentTemplate template)
        : base(id, template) 
    {
    }

    public override void OnTick(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        if (!_resolvedDamagePerTick.HasValue)
            throw new InvalidOperationException("Damage per tick was not resolved before ticking.");

        // tick damage is multiplied per stack
        var damage = _resolvedDamagePerTick.Value * effect.CurrentStacks;

        context.Units.ChangeHp(effect.TargetUnitId, -damage);
    }
}
