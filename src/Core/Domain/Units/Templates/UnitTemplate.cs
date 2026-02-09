using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Instances.Mutable;

namespace Core.Domain.Units.Templates
{
    /// <summary>
    /// Defines the static, unchanging data for a unit type
    /// </summary>
    /// <remarks>
    /// Used as a blueprint when creating runtime <see cref="UnitInstance"/> instances
    /// Stores stable identifiers, display name, core <see cref="UnitBaseStats"/> and an array of <see cref="Abilities.Ability"/> Ids.
    /// </remarks>
    public class UnitTemplate
    {
        public UnitTemplateId Id { get; }

        public string Name { get; }

        public UnitBaseStats BaseStats { get; }

        public AbilityId[] AbilityIds { get; }

        public UnitTemplate(UnitTemplateId id, string name, UnitBaseStats baseStats, AbilityId[] abilityIds)
        {
            Id = id;
            Name = name;
            BaseStats = baseStats;
            AbilityIds = abilityIds;
        }
    }
}

