namespace Core.Effects.Templates;
using Core.Effects.Instances;
using Core.Types;
/// <summary>
/// Defines the static data and behavior blueprint for an effect, including duration,
/// stack limits, and its component templates
/// </summary>
/// <remarks>
/// Acts as a blueprint for creating runtime <see cref="EffectInstance"/> objects.
/// Aggregates one or more <see cref="EffectComponentTemplate"/>s that define
/// the behavior applied over the effect's lifetime.
/// </remarks>
public abstract class EffectTemplate
{
    public EffectTemplateId Id { get; }
    public string Name { get; }
    public bool IsHarmful { get; }
    public int TotalTicks { get; }
    public int MaxStacks { get; }
    public IReadOnlyList<EffectComponentTemplate> Components { get; }

    /// <summary>
    /// Creates a template with a configuration and components
    /// Holds static data for effect instances
    /// </summary>
    protected EffectTemplate(
        EffectTemplateId id,
        string name,
        bool isHarmful,
        int totalTicks,
        int maxStacks,
        IEnumerable<EffectComponentTemplate> components)
    {
        Id = id;
        Name = name;
        IsHarmful = isHarmful;
        TotalTicks = totalTicks;
        MaxStacks = maxStacks;
        Components = components.ToList();
    }

    /// <summary>
    /// Generates a new effect instance using this template as the blueprint
    /// </summary>
    public virtual EffectInstance CreateInstance(string sourceUnitId, string targetUnitId)
    {
        var instanceId = new EffectInstanceId(Guid.NewGuid().GetHashCode());
        return new EffectInstance(instanceId, this, sourceUnitId, targetUnitId);
    }
}

