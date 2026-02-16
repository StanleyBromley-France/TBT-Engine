namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances;
using Core.Engine.Mutation;
using Core.Domain.Types;

public sealed class DamageOverTimeComponentInstance : EffectComponentInstance<DamageOverTimeComponentTemplate>
{
    public DamageOverTimeComponentInstance(
        EffectComponentInstanceId id,
        DamageOverTimeComponentTemplate template)
        : base(id, template) { }

    public override void OnTick(GameMutationContext context, EffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        // tick damage is multiplied per stack
        var stacks = Math.Max(1, effect.CurrentStacks);
        var damage = TemplateTyped.DamagePerTick * stacks;

        foreach (var target in effect.TargetUnitIds)
            context.Units.ChangeHp(target, -damage);
    }
}
