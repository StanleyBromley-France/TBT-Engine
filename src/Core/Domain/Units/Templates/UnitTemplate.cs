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
    /// Stores stable identifiers, display name, core <see cref="UnitStats"/> and an array of <see cref="Abilities.Ability"/> Id strings.
    /// </remarks>
    public class UnitTemplate
    {
        public UnitTemplateId Id { get; set; }

        public string Name { get; set; }

        public UnitStats BaseStats { get; set; }

        public String[] AbilityIds { get; set; }

        public UnitTemplate(UnitTemplateId id, string name, UnitStats baseStats, String[] abilityIds)
        {
            Id = id;
            Name = name;
            BaseStats = baseStats;
            AbilityIds = abilityIds;
        }
    }
}

