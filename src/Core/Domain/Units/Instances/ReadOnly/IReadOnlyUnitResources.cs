using Core.Domain.Units.Instances.Mutable;

namespace Core.Domain.Units.Instances.ReadOnly;

/// <summary>
/// Read-only view of a unit’s <see cref="UnitResources"/>.
/// Exposes current resource values without allowing direct mutation
/// outside of GameMutationContext.
/// </summary>

public interface IReadOnlyUnitResources
{
    int HP { get; }
    int MovePoints { get; }
    int ActionPoints { get; }
    int Mana { get; }
}
