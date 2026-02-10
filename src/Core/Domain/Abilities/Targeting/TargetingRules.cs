namespace Core.Domain.Abilities.Targeting;

/// <summary>
/// Describes how an ability selects valid targets
/// </summary>
/// <remarks>
/// Works together with <see cref="TargetType"/> and optionally
/// <see cref="AreaPattern"/> to define how an ability acquires targets.
/// Referenced by <see cref="Ability"/> to enforce targeting constraints.
/// </remarks>
public sealed class TargetingRules
{
    private readonly TargetType[] _allowedTargets;
    public int Range { get; }
    public bool RequiresLineOfSight { get; }
    public IReadOnlyList<TargetType> AllowedTargets => _allowedTargets;
    public AreaPattern? Pattern { get; }

    /// <summary>
    /// Initializes targeting rules with distance, visibility, allowed targets,
    /// optional area targeting, and self-targeting behavior.
    /// </summary>
    public TargetingRules(
        int range,
        bool requiresLineOfSight,
        IEnumerable<TargetType> allowedTargets,
        AreaPattern? areaPattern = null)
    {
        Range = range;
        RequiresLineOfSight = requiresLineOfSight;
        _allowedTargets = allowedTargets.ToArray();
        Pattern = areaPattern;
    }
}
