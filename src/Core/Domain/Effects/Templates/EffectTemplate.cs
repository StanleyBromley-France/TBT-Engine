namespace Core.Domain.Effects.Templates;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Types;
/// <summary>
/// Defines the static data and behavior blueprint for an effect, including duration,
/// stack limits, and its component templates
/// </summary>
/// <remarks>
/// Acts as a blueprint for creating runtime <see cref="EffectInstance"/> objects.
/// Aggregates one or more <see cref="EffectComponentTemplate"/>s that define
/// the behavior applied over the effect's lifetime.
/// </remarks>
public class EffectTemplate
{
    private readonly EffectComponentTemplateId[] _components;
    public EffectTemplateId Id { get; }
    public string Name { get; }
    public bool IsHarmful { get; }
    public int TotalTicks { get; }
    public int MaxStacks { get; }
    public IReadOnlyList<EffectComponentTemplateId> Components => _components;

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
        IEnumerable<EffectComponentTemplateId> components)
    {
        Id = id;
        Name = name;
        IsHarmful = isHarmful;
        TotalTicks = totalTicks;
        MaxStacks = maxStacks;
        _components = components.ToArray();
    }
}

