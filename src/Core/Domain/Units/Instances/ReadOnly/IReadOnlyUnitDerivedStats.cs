using Core.Domain.Units.Instances.Mutable;

namespace Core.Domain.Units.Instances.ReadOnly;

/// <summary>
/// Read-only view of a unit’s <see cref="UnitDerivedStats"/>.
/// Exposes computed stat values without allowing mutation
/// outside of GameMutationContext.
/// </summary>
public interface IReadOnlyUnitDerivedStats
{
    int MovePoints { get; }
    int ArmourPoints { get; }
    int MagicResistance { get; }
    int MaxHP { get; }
    int MaxManaPoints { get; }
    int ActionPoints { get; }
    int HealingReceived { get; }
    int HealingDealt { get; }
    int DamageTaken { get; }
    int DamageDealt { get; }
}
