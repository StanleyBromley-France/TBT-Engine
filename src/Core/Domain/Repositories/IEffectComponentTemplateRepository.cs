namespace Core.Domain.Repositories;

using Types;
using Effects.Templates;

/// <summary>
/// Provides read-only access to compiled and validated <see cref="EffectComponentTemplate"/> definitions.
/// </summary>
public interface IEffectComponentTemplateRepository
{
    /// <summary>
    /// Retrieves the effect component template associated with the given identifier.
    /// 
    /// Implementations may throw if the identifier is unknown.
    /// Callers in validation paths should prefer <see cref="TryGet"/>.
    /// </summary>
    public EffectComponentTemplate Get(EffectComponentTemplateId id);

    /// <summary>
    /// Attempts to retrieve the effect component template associated with the given identifier.
    /// Returns <c>true</c> if found; otherwise <c>false</c>.
    /// </summary>
    bool TryGet(EffectComponentTemplateId id, out EffectComponentTemplate template);

    /// <summary>
    /// Returns a read-only view of all available effect component templates.
    /// </summary>
    public IReadOnlyDictionary<EffectComponentTemplateId, EffectComponentTemplate> GetAll();
}
