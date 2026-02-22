namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Effects.Components.Instances.ReadOnly;

public sealed class InstantHealComponentInstance : EffectComponentInstance<InstantHealComponentTemplate>, IReadOnlyResolvableHpDeltaComponent
{
    private int? _resolvedHeal;
    int? IResolvableHpDeltaComponent.ResolvedHpDelta
    {
        get => _resolvedHeal;
        set => _resolvedHeal = value;
    }

    HpType IReadOnlyResolvableHpDeltaComponent.HpType => HpType.Heal;

    public InstantHealComponentInstance(
        EffectComponentInstanceId id,
        InstantHealComponentTemplate template)
        : base(id, template) {}

    public override void OnApply(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        if (!_resolvedHeal.HasValue)
            throw new InvalidOperationException("Heal was not resolved before applying.");

        context.Units.ChangeHp(effect.TargetUnitId, _resolvedHeal.Value);
    }
}
