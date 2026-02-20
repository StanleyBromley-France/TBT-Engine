namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Templates;
using Core.Engine.Mutation;
using Core.Domain.Types;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Game;

public sealed class DamageOverTimeComponentInstance : EffectComponentInstance<DamageOverTimeComponentTemplate>
{
    private readonly int _resolvedDamagePerTick;

    public DamageOverTimeComponentInstance(
        EffectComponentInstanceId id,
        DamageOverTimeComponentTemplate template,
        int resolvedDamagePerTick)
        : base(id, template) 
    {
        _resolvedDamagePerTick = resolvedDamagePerTick;
    }

    public override void OnTick(GameMutationContext context, IReadOnlyEffectInstance effect)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        // tick damage is multiplied per stack
        var damage = _resolvedDamagePerTick * effect.CurrentStacks;

        context.Units.ChangeHp(effect.TargetUnitId, -damage);
    }
}
