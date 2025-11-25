using Core.Units.Instances;

namespace Core.Units.Templates
{
    /// <summary>
    /// Defines the static, unchanging data for a unit type
    /// </summary>
    /// <remarks>
    /// Used as a blueprint when creating runtime <see cref="UnitInstance"/> instances
    /// Stores stable identifiers, display name, core <see cref="UnitStats"/> and a list of <see cref="Abilities.Ability"/>.
    /// </remarks>
    public class UnitTemplate
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public UnitStats BaseStats { get; set; }

        public List<Abilities.Ability> Abilities { get; set; }

        public UnitTemplate()
        {
            Id = string.Empty;
            Name = string.Empty;
            BaseStats = new UnitStats();
            Abilities = new List<Abilities.Ability>();
        }

        public UnitTemplate(string id, string name, UnitStats baseStats, List<Abilities.Ability> abilities)
        {
            Id = id;
            Name = name;
            BaseStats = baseStats;
            Abilities = abilities;
        }
    }
}

