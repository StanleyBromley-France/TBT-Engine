using Core.Domain.Units.Instances.Mutable;

namespace Core.Domain.Units.Instances.ReadOnly;

/// <summary> 
/// Read-only view of a unit’s <see cref="UnitDerivedStats"/>.
/// Exposes computed stat values without allowing mutation
/// outside of GameMutationContext.
/// </summary>
public interface IReadOnlyUnitDerivedStats
{
    // Core stats
    int MaxHP { get; }
    int MaxManaPoints { get; }
    int MovePoints { get; }
    int ActionPoints { get; }

    // Percentage modifiers (100 = normal effectiveness)

    // Outgoing
    int DamageDealt { get; }
    int HealingDealt { get; }

    // Incoming
    int HealingReceived { get; }

    int PhysicalDamageReceived { get; }
    int MagicDamageReceived { get; }
}