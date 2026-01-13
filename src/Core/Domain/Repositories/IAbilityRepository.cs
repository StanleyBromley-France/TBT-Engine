namespace Core.Domain.Repositories;

using Types;
using Abilities;

/// <summary>
/// Provides read-only access to compiled and validated <see cref="Ability"/> definitions.
/// </summary>
public interface IAbilityRepository
{
    /// <summary>
    /// Retrieves the ability associated with the given identifier.
    /// 
    /// Implementations may throw if the identifier is unknown.
    /// Callers in validation paths should prefer <see cref="TryGet"/>.
    /// </summary>
    public Ability Get(AbilityId id);

    /// <summary>
    /// Attempts to retrieve the ability associated with the given identifier.
    /// Returns <c>true</c> if found; otherwise <c>false</c>.
    /// </summary>
    bool TryGet(AbilityId id, out Ability ability);

    /// <summary>
    /// Returns a read-only view of all available abilities.
    /// </summary>
    public IReadOnlyDictionary<AbilityId, Ability> GetAll();
}
