using Core.Units.Instances;

namespace Core.Units.Templates
{
    /// <summary>
    /// Defines the static, unchanging data for a unit type
    /// </summary>
    /// <remarks>
    /// Used as a blueprint when creating runtime <see cref="UnitInstance"/> instances
    /// Stores stable identifiers, display name, and core <see cref="UnitStats"/>
    /// </remarks>
    public class UnitTemplate
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public UnitStats BaseStats { get; set; }

        // TODO: Add List<Ability> Abilities

        public UnitTemplate()
        {
            Id = string.Empty;
            Name = string.Empty;
            BaseStats = new UnitStats();
        }

        public UnitTemplate(string id, string name, UnitStats baseStats)
        {
            Id = id;
            Name = name;
            BaseStats = baseStats;
        }
    }
}

