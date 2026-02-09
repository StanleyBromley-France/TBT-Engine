namespace Core.Domain.Units;

/// <summary>
/// Read-only view of a unit’s <see cref="UnitDerivedStats"/>.
/// Exposes computed stat values without allowing mutation
/// outside of GameMutationContext.
/// </summary>

public interface IReadOnlyUnitDerivedStats
{
    int MagicResistance { get; }
    int Armor { get; }
}
