namespace Core.Domain.Repositories;

using Types;
using Units.Templates;

/// <summary>
/// Provides read-only access to compiled and validated <see cref="UnitTemplate"/> definitions.
/// </summary>
public interface IUnitTemplateRepository
{
    /// <summary>
    /// Retrieves the unit template associated with the given identifier.
    /// 
    /// Implementations may throw if the identifier is unknown.
    /// Callers in validation paths should prefer <see cref="TryGet"/>.
    /// </summary>
    public UnitTemplate Get(UnitTemplateId id);

    /// <summary>
    /// Attempts to retrieve the unit template associated with the given identifier.
    /// Returns <c>true</c> if found; otherwise <c>false</c>.
    /// </summary>
    bool TryGet(UnitTemplateId id, out UnitTemplate template);

    /// <summary>
    /// Returns a read-only view of all available unit templates.
    /// </summary>
    public IReadOnlyDictionary<UnitTemplateId, UnitTemplate> GetAll();
}
