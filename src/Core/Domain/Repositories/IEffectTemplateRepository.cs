namespace Core.Domain.Repositories;

using Types;
using Effects.Templates;

/// <summary>
/// Provides read-only access to compiled and validated <see cref="EffectTemplate"/> definitions.
/// </summary>
public interface IEffectTemplateRepository
{
    /// <summary>
    /// Retrieves the effect template associated with the given identifier.
    /// 
    /// Implementations may throw if the identifier is unknown.
    /// Callers in validation paths should prefer <see cref="TryGet"/>.
    /// </summary>
    public EffectTemplate Get(EffectTemplateId id);

    /// <summary>
    /// Attempts to retrieve the effect template associated with the given identifier.
    /// Returns <c>true</c> if found; otherwise <c>false</c>.
    /// </summary>
    bool TryGet(EffectTemplateId id, out EffectTemplate template);

    /// <summary>
    /// Returns a read-only view of all available effect templates.
    /// </summary>
    public IReadOnlyDictionary<EffectTemplateId, EffectTemplate> GetAll();
}
