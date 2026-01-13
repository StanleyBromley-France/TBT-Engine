namespace Core.Domain.Effects.Instances;

using Core.Domain.Effects.Templates;
using Core.Domain.Types;
using Core.Game;

/// <summary>
/// Represents a single active effect applied from one unit to another,
/// tracking duration, stack count, and component behaviors
/// </summary>
/// <remarks>
/// Created from an <see cref="EffectTemplate"/> and applied to a target unit.
/// Delegates its behavior to the underlying <see cref="EffectComponentTemplate"/>s
/// on initial application and on each tick.
/// </remarks>
public sealed class EffectInstance
{
    public EffectInstanceId Id { get; }
    public EffectTemplate Template { get; }
    public string SourceUnitId { get; }
    public string TargetUnitId { get; }
    public int RemainingTicks { get; private set; }
    public int CurrentStacks { get; private set; }

    public IReadOnlyList<EffectComponentTemplate> Components => Template.Components;

    /// <summary>
    /// Creates a new effect instance using the given template and unit IDs. 
    /// Starts with one stack
    /// </summary>
    public EffectInstance(
        EffectInstanceId id,
        EffectTemplate template,
        string sourceUnitId,
        string targetUnitId)
    {
        Id = id;
        Template = template;
        SourceUnitId = sourceUnitId;
        TargetUnitId = targetUnitId;
        RemainingTicks = template.TotalTicks;
        CurrentStacks = 1;
    }

    /// <summary>
    /// Applies all initial component effects when the instance is first used
    /// </summary>
    public GameState ApplyInitial(GameState state)
    {
        foreach (var component in Components)
        {
            state = component.ApplyInitial(state, SourceUnitId, TargetUnitId);
        }

        return state;
    }

    /// <summary>
    /// Advances the effect by one tick, triggering component tick behaviors
    /// and reducing remaining duration
    /// </summary>
    public GameState Tick(GameState state)
    {
        if (RemainingTicks <= 0)
            return state;

        RemainingTicks--;

        foreach (var component in Components)
        {
            state = component.Tick(state, SourceUnitId, TargetUnitId);
        }

        return state;
    }

    /// <summary>
    /// Increases the stack count if the effect has not yet reached its maximum allowed stacks.
    /// Returns true if stacks were allowed to increase and false if not
    /// </summary>
    public bool IncrementStack()
    {
        if (CurrentStacks < Template.MaxStacks)
        {
            CurrentStacks++;
            return true;
        }
        return false;
    }
}

