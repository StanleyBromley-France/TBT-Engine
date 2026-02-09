namespace Core.Domain.Units.Instances.ReadOnly;

using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;

/// <summary>
/// Read-only view of a <see cref="UnitInstance"/>.
/// Exposes immutable identity data and read-only projections of the unit’s
/// mutable runtime state, preventing direct mutation outside of
/// GameMutationContext.
/// </summary>

public interface IReadOnlyUnitInstance
{
    UnitInstanceId Id { get; }
    TeamId Team { get; }
    UnitTemplate Template { get; }

    IReadOnlyUnitResources Resources { get; }
    IReadOnlyUnitDerivedStats DerivedStats { get; }

    HexCoord Position { get; }

    bool IsAlive { get; }
}
