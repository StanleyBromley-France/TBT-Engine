namespace Core.Domain.Repositories;

using System;
using System.Collections.Generic;
using Core.Domain.Abilities;
using Core.Domain.Types;

/// <summary>
/// In-memory read-only repository for compiled/validated <see cref="Ability"/> definitions.
/// </summary>
public sealed class AbilityRepository : IAbilityRepository
{
    private readonly IReadOnlyDictionary<AbilityId, Ability> _abilities;

    public AbilityRepository(IReadOnlyDictionary<AbilityId, Ability> abilities)
    {
        _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
    }

    /// <summary>
    /// Convenience ctor for callers that have an enumerable.
    /// </summary>
    public AbilityRepository(IEnumerable<KeyValuePair<AbilityId, Ability>> abilities)
    {
        if (abilities is null) throw new ArgumentNullException(nameof(abilities));

        var dict = new Dictionary<AbilityId, Ability>();
        foreach (var (id, ability) in abilities)
        {
            if (ability is null)
                throw new ArgumentException($"Ability value is null for id '{id}'.", nameof(abilities));

            if (!dict.TryAdd(id, ability))
                throw new ArgumentException($"Duplicate ability id '{id}'.", nameof(abilities));
        }

        _abilities = dict;
    }

    public Ability Get(AbilityId id)
    {
        if (_abilities.TryGetValue(id, out var ability))
            return ability;

        throw new KeyNotFoundException($"Unknown ability id '{id}'.");
    }

    public bool TryGet(AbilityId id, out Ability ability)
        => _abilities.TryGetValue(id, out ability!);

    public IReadOnlyDictionary<AbilityId, Ability> GetAll()
        => _abilities;
}