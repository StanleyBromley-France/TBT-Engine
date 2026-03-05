namespace Core.Domain.Repositories;

using System;
using System.Collections.Generic;
using Core.Domain.Types;
using Core.Domain.Effects.Components.Templates;

/// <summary>
/// In-memory read-only repository for compiled/validated <see cref="EffectComponentTemplate"/> definitions.
/// </summary>
public sealed class EffectComponentTemplateRepository : IEffectComponentTemplateRepository
{
    private readonly IReadOnlyDictionary<EffectComponentTemplateId, EffectComponentTemplate> _templates;

    public EffectComponentTemplateRepository(
        IReadOnlyDictionary<EffectComponentTemplateId, EffectComponentTemplate> templates)
    {
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    /// <summary>
    /// Convenience constructor for enumerable inputs.
    /// </summary>
    public EffectComponentTemplateRepository(
        IEnumerable<KeyValuePair<EffectComponentTemplateId, EffectComponentTemplate>> templates)
    {
        if (templates is null)
            throw new ArgumentNullException(nameof(templates));

        var dict = new Dictionary<EffectComponentTemplateId, EffectComponentTemplate>();

        foreach (var (id, template) in templates)
        {
            if (template is null)
                throw new ArgumentException($"Template value is null for id '{id}'.", nameof(templates));

            if (!dict.TryAdd(id, template))
                throw new ArgumentException($"Duplicate template id '{id}'.", nameof(templates));
        }

        _templates = dict;
    }

    public EffectComponentTemplate Get(EffectComponentTemplateId id)
    {
        if (_templates.TryGetValue(id, out var template))
            return template;

        throw new KeyNotFoundException($"Unknown effect component template id '{id}'.");
    }

    public bool TryGet(EffectComponentTemplateId id, out EffectComponentTemplate template)
        => _templates.TryGetValue(id, out template!);

    public IReadOnlyDictionary<EffectComponentTemplateId, EffectComponentTemplate> GetAll()
        => _templates;
}