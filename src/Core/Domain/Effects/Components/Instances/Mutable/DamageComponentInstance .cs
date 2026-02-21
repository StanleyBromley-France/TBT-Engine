namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Engine.Mutation;

public sealed class DamageComponentInstance : EffectComponentInstance<InstantDamageComponentTemplate>
{
    private readonly int _resolvedDamage;
    public DamageComponentInstance(Types.EffectComponentInstanceId id, InstantDamageComponentTemplate template, int resolvedDamage)
        : base(id, template) 
    {
        _resolvedDamage = resolvedDamage;
    }

    public override void OnApply(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        context.Units.ChangeHp(effect.TargetUnitId, -_resolvedDamage);
    }
}
