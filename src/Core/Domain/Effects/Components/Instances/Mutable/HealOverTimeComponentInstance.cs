namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.ReadOnly;

public sealed class HealOverTimeComponentInstance
    : EffectComponentInstance<HealOverTimeComponentTemplate>
{
    private readonly int _resolvedHealPerTick;

    public HealOverTimeComponentInstance(
        EffectComponentInstanceId id,
        HealOverTimeComponentTemplate template)
        : base(id, template) { }

    public override void OnTick(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        var heal = _resolvedHealPerTick * effect.CurrentStacks;

        context.Units.ChangeHp(effect.TargetUnitId, +heal);
    }
}
