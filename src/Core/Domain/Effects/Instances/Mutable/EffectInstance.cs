namespace Core.Domain.Effects.Instances.Mutable;

using Components.Instances.Mutable;
using Components.Instances.ReadOnly;
using Core.Domain.Effects.Instances.Execution;
using Domain.Types;
using Effects.Instances.ReadOnly;
using Effects.Templates;
using Engine.Mutation;

/// <summary>
/// Represents a single active effect applied from one unit to another,
/// tracking duration, stack count, and component behaviors
/// </summary>
/// <remarks>
/// Created from an <see cref="Templates.EffectTemplate"/> and applied to a target unit.
/// Delegates its behavior to the underlying <see cref="EffectComponentInstance"/>s
/// on initial application and on each tick.
/// </remarks>
public sealed class EffectInstance : IReadOnlyEffectInstance, IEffectInstanceExecution
{
    public EffectInstanceId Id { get; }
    public Templates.EffectTemplate Template { get; }
    public UnitInstanceId SourceUnitId { get; }
    public UnitInstanceId TargetUnitId { get; }
    public int RemainingTicks { get; set; }
    public int CurrentStacks { get; set; }
    public EffectComponentInstance[] Components { get; }

    IReadOnlyEffectComponentInstance[] IReadOnlyEffectInstance.Components => Components;

    public EffectInstance(
        EffectInstanceId id,
        Templates.EffectTemplate template,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId,
        EffectComponentInstance[] components)
    {
        Id = id;
        Template = template;
        SourceUnitId = sourceUnitId;
        TargetUnitId = targetUnitId;
        RemainingTicks = Template.TotalTicks;
        CurrentStacks = 1;
        Components = components;
    }

    /// <summary>
    /// Applies all initial component effects when the instance is first used
    /// </summary>
    void IEffectInstanceExecution.OnApply(GameMutationContext cxt)
    {
        foreach (var component in Components)
        {
            component.OnApply(cxt, this);
        }
    }

    /// <summary>
    /// Triggers component tick behaviors.
    /// Does not reduce tick count, that should be done through Effect mutation TickAll()
    /// </summary>
    void IEffectInstanceExecution.OnTick(GameMutationContext cxt)
    {
        if (RemainingTicks <= 0)
            return;

        foreach (var component in Components)
        {
            component.OnTick(cxt, this);
        }
    }

    void IEffectInstanceExecution.OnExpire(GameMutationContext cxt)
    {
        foreach (var component in Components)
        {
            component.OnExpire(cxt, this);
        }
    }

    public bool IsExpired()
    {
        return RemainingTicks <= 0;
    }
}

