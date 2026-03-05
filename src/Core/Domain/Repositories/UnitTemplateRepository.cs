namespace Core.Domain.Repositories;

using Core.Domain.Types;
using Core.Domain.Units.Templates;

/// <summary>
/// In-memory read-only repository for compiled/validated <see cref="UnitTemplate"/> definitions.
/// </summary>
public sealed class UnitTemplateRepository : IUnitTemplateRepository
{
    private readonly IReadOnlyDictionary<UnitTemplateId, UnitTemplate> _templates;

    public UnitTemplateRepository(IReadOnlyDictionary<UnitTemplateId, UnitTemplate> templates)
    {
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    /// <summary>
    /// Convenience constructor for enumerable inputs.
    /// </summary>
    public UnitTemplateRepository(IEnumerable<KeyValuePair<UnitTemplateId, UnitTemplate>> templates)
    {
        if (templates is null)
            throw new ArgumentNullException(nameof(templates));

        var dict = new Dictionary<UnitTemplateId, UnitTemplate>();

        foreach (var (id, template) in templates)
        {
            if (template is null)
                throw new ArgumentException($"Template value is null for id '{id}'.", nameof(templates));

            if (!dict.TryAdd(id, template))
                throw new ArgumentException($"Duplicate unit template id '{id}'.", nameof(templates));
        }

        _templates = dict;
    }

    public UnitTemplate Get(UnitTemplateId id)
    {
        if (_templates.TryGetValue(id, out var template))
            return template;

        throw new KeyNotFoundException($"Unknown unit template id '{id}'.");
    }

    public bool TryGet(UnitTemplateId id, out UnitTemplate template)
        => _templates.TryGetValue(id, out template!);

    public IReadOnlyDictionary<UnitTemplateId, UnitTemplate> GetAll()
        => _templates;
}
