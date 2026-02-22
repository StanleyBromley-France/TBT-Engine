namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Engine.Mutation;
using Types;

public sealed class InstantDamageComponentInstance : EffectComponentInstance<InstantDamageComponentTemplate>, IResolvableHpDeltaComponent
{
    private int? _resolvedDamage;

    int? IResolvableHpDeltaComponent.ResolvedHpDelta
    {
        get => _resolvedDamage;
        set => _resolvedDamage = value;
    }
    public InstantDamageComponentInstance(
        EffectComponentInstanceId id, 
        InstantDamageComponentTemplate template)
        : base(id, template) 
    {
    }

    public override void OnApply(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        if (!_resolvedDamage.HasValue)
            throw new InvalidOperationException("Damage was not resolved before applying.");

        context.Units.ChangeHp(effect.TargetUnitId, -_resolvedDamage.Value);
    }
}
