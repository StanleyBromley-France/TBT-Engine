namespace Core.Domain.Repositories;

using System;
using System.Collections.Generic;
using Core.Domain.Types;
using Core.Domain.Effects.Templates;

/// <summary>
/// In-memory read-only repository for compiled/validated <see cref="EffectTemplate"/> definitions.
/// </summary>
public sealed class EffectTemplateRepository : IEffectTemplateRepository
{
    private readonly IReadOnlyDictionary<EffectTemplateId, EffectTemplate> _templates;

    public EffectTemplateRepository(
        IReadOnlyDictionary<EffectTemplateId, EffectTemplate> templates)
    {
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    /// <summary>
    /// Convenience constructor for enumerable inputs.
    /// </summary>
    public EffectTemplateRepository(
        IEnumerable<KeyValuePair<EffectTemplateId, EffectTemplate>> templates)
    {
        if (templates is null)
            throw new ArgumentNullException(nameof(templates));

        var dict = new Dictionary<EffectTemplateId, EffectTemplate>();

        foreach (var (id, template) in templates)
        {
            if (template is null)
                throw new ArgumentException($"Template value is null for id '{id}'.", nameof(templates));

            if (!dict.TryAdd(id, template))
                throw new ArgumentException($"Duplicate effect template id '{id}'.", nameof(templates));
        }

        _templates = dict;
    }

    public EffectTemplate Get(EffectTemplateId id)
    {
        if (_templates.TryGetValue(id, out var template))
            return template;

        throw new KeyNotFoundException($"Unknown effect template id '{id}'.");
    }

    public bool TryGet(EffectTemplateId id, out EffectTemplate template)
        => _templates.TryGetValue(id, out template!);

    public IReadOnlyDictionary<EffectTemplateId, EffectTemplate> GetAll()
        => _templates;
}