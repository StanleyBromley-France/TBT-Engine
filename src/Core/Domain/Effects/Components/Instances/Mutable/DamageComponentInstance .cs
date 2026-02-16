namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances;
using Core.Engine.Mutation;

public sealed class DamageComponentInstance : EffectComponentInstance<DamageComponentTemplate>
{
    public DamageComponentInstance(Types.EffectComponentInstanceId id, DamageComponentTemplate template)
        : base(id, template) { }

    public override void OnApply(GameMutationContext context, EffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        foreach (var target in effect.TargetUnitIds)
            context.Units.ChangeHp(target, -TemplateTyped.Damage);
    }
}
