namespace Core.Domain.Effects.Instances;

using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Templates;
using Core.Domain.Types;
using Core.Engine;
using Core.Game;

/// <summary>
/// Represents a single active effect applied from one unit to another,
/// tracking duration, stack count, and component behaviors
/// </summary>
/// <remarks>
/// Created from an <see cref="EffectTemplate"/> and applied to a target unit.
/// Delegates its behavior to the underlying <see cref="EffectComponentInstance"/>s
/// on initial application and on each tick.
/// </remarks>
public sealed class EffectInstance
{
    private readonly EffectComponentInstance[] _components;

    public EffectInstanceId Id { get; }
    public EffectTemplate Template { get; }
    public UnitInstanceId SourceUnitId { get; }
    public UnitInstanceId TargetUnitId { get; }
    public int RemainingTicks { get; private set; }
    public int CurrentStacks { get; private set; }
    public IReadOnlyList<EffectComponentInstance> Components => _components;
    /// <summary>
    /// Creates a new effect instance using the given template and unit IDs. 
    /// Starts with one stack
    /// </summary>
    public EffectInstance(
        EffectInstanceId id,
        EffectTemplate template,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId,
        IEnumerable<EffectComponentInstance> components)
    {
        Id = id;
        Template = template;
        SourceUnitId = sourceUnitId;
        TargetUnitId = targetUnitId;

        RemainingTicks = template.TotalTicks;
        CurrentStacks = 1;

        _components = (components ?? throw new ArgumentNullException(nameof(components))).ToArray();
    }

    /// <summary>
    /// Applies all initial component effects when the instance is first used
    /// </summary>
    public void OnApply(GameMutationContext cxt)
    {
        foreach (var component in Components)
        {
            component.OnApply(cxt, this);
        }
    }

    /// <summary>
    /// Advances the effect by one tick, triggering component tick behaviors
    /// and reducing remaining duration
    /// </summary>
    public void OnTick(GameMutationContext cxt)
    {
        if (RemainingTicks <= 0)
            return;

        RemainingTicks--;

        foreach (var component in Components)
        {
            component.OnTick(cxt, this);
        }
    }

    public void OnExpire(GameMutationContext cxt)
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

