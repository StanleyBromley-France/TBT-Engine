namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.Mutable;

public sealed class HealOverTimeComponentInstance
    : EffectComponentInstance<HealOverTimeComponentTemplate>
{
    public HealOverTimeComponentInstance(
        EffectComponentInstanceId id,
        HealOverTimeComponentTemplate template)
        : base(id, template) { }

    public override void OnTick(GameMutationContext context, EffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        var stacks = Math.Max(1, effect.CurrentStacks);
        var heal = TemplateTyped.HealPerTick * stacks;

        foreach (var target in effect.TargetUnitIds)
            context.Units.ChangeHp(target, +heal);
    }
}
