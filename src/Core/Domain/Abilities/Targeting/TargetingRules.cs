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
    public int Range { get; }
    public bool RequiresLineOfSight { get; }
    public TargetType AllowedTarget { get; }
    public int MinTargets { get; }
    public int MaxTargets { get; }
    /// <summary>
    /// Initializes targeting rules with distance, visibility, allowed targets,
    /// optional area targeting, and self-targeting behavior.
    /// </summary>
    public TargetingRules(
        int range,
        bool requiresLineOfSight,
        TargetType allowedTarget,
        int minTargets,
        int maxTargets)
    {
        Range = range;
        RequiresLineOfSight = requiresLineOfSight;
        AllowedTarget = allowedTarget;
        MinTargets = minTargets;
        MaxTargets = maxTargets;
    }
}
