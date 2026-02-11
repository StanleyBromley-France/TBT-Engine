namespace Core.Domain.Effects.Components.Instances.Mutable;

using Core.Domain.Effects.Components.Instances.ReadOnly;
using Core.Domain.Effects.Instances;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Core.Engine.Mutation;

/// <summary>
/// Base class for all runtime effect component instances.
/// Implements the read-only contract; mutation hooks are no-ops by default.
/// </summary>
public abstract class EffectComponentInstance : IReadOnlyEffectComponentInstance
{
    public EffectComponentInstanceId Id { get; }
    public EffectComponentTemplate Template { get; }

    protected EffectComponentInstance(
        EffectComponentInstanceId id,
        EffectComponentTemplate template)
    {
        Id = id;
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }

    public virtual void OnApply(GameMutationContext context, EffectInstance effect) { }
    public virtual void OnTick(GameMutationContext context, EffectInstance effect) { }
    public virtual void OnExpire(GameMutationContext context, EffectInstance effect) { }
}
