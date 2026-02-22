namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Effects.Components.Instances.ReadOnly;

public sealed class HealOverTimeComponentInstance
    : EffectComponentInstance<HealOverTimeComponentTemplate>, IReadOnlyResolvableHpDeltaComponent
{
    private int? _resolvedHealPerTick;
    int? IResolvableHpDeltaComponent.ResolvedHpDelta
    {
        get => _resolvedHealPerTick;
        set => _resolvedHealPerTick = value;
    }

    HpType IReadOnlyResolvableHpDeltaComponent.HpType => HpType.Heal;

    public HealOverTimeComponentInstance(
        EffectComponentInstanceId id,
        HealOverTimeComponentTemplate template)
        : base(id, template) { }

    public override void OnTick(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        if (!_resolvedHealPerTick.HasValue)
            throw new InvalidOperationException("Heal per tick was not resolved before ticking.");

        var heal = _resolvedHealPerTick.Value * effect.CurrentStacks;

        context.Units.ChangeHp(effect.TargetUnitId, +heal);
    }
}
