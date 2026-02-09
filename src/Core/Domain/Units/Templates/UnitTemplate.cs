using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Instances;

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
        public UnitTemplateId Id { get; set; }

        public string Name { get; set; }

        public UnitBaseStats BaseStats { get; set; }

        public AbilityId[] AbilityIds { get; set; }

        public UnitTemplate(UnitTemplateId id, string name, UnitBaseStats baseStats, AbilityId[] abilityIds)
        {
            Id = id;
            Name = name;
            BaseStats = baseStats;
            AbilityIds = abilityIds;
        }
    }
}

