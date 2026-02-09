namespace Core.Domain.Abilities;

using Core.Domain.Types;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Effects.Templates;

/// <summary>
/// Defines an ability that can be executed by a unit, including its identity,
/// category, cost, targeting rules, and associated effect templates.
/// </summary>
/// <remarks>
/// Uses <see cref="AbilityCost"/> for resource requirements and
/// <see cref="TargetingRules"/> to determine valid targets.
/// Used to apply one or more <see cref="EffectTemplate"/> instances on a target.
/// </remarks>
public sealed class Ability
{
    public AbilityId Id { get; }
    public string Name { get; }
    public AbilityCategory Category { get; }
    public int ManaCost { get; }
    public TargetingRules Targeting { get; }
    public IReadOnlyList<EffectTemplateId> Effects { get; }

    /// <summary>
    /// Creates a new ability using the specified identifiers, metadata, cost,
    /// targeting configuration, and effects.
    /// </summary>
    public Ability(
        AbilityId id,
        string name,
        AbilityCategory category,
        int cost,
        TargetingRules targeting,
        IEnumerable<EffectTemplateId> effects)
    {
        Id = id;
        Name = name;
        Category = category;
        ManaCost = cost;
        Targeting = targeting;
        Effects = new List<EffectTemplateId>(effects);
    }
}
