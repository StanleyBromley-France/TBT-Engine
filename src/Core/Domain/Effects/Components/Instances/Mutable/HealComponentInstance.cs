namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.ReadOnly;

public sealed class HealComponentInstance
    : EffectComponentInstance<HealComponentTemplate>
{
    private readonly int _resolvedHeal;

    public HealComponentInstance(
        EffectComponentInstanceId id,
        HealComponentTemplate template,
        int resolvedHeal)
        : base(id, template) 
    {
        _resolvedHeal = resolvedHeal;
    }

    public override void OnApply(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        context.Units.ChangeHp(effect.TargetUnitId, _resolvedHeal);
    }
}
