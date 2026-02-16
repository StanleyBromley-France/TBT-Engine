namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.Mutable;

public sealed class HealComponentInstance
    : EffectComponentInstance<HealComponentTemplate>
{
    public HealComponentInstance(
        EffectComponentInstanceId id,
        HealComponentTemplate template)
        : base(id, template) { }

    public override void OnApply(GameMutationContext context, EffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        foreach (var target in effect.TargetUnitIds)
            context.Units.ChangeHp(target, TemplateTyped.Heal);
    }
}
