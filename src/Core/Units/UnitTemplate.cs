namespace Core.Units
{
    /// <summary>
    /// Defines the static, unchanging data for a unit type
    /// </summary>
    /// <remarks>
    /// Used as a blueprint when creating runtime <see cref="Unit"/> instances
    /// Stores stable identifiers, display name, and core <see cref="UnitStats"/>
    /// </remarks>
    public class UnitTemplate
    {
        /// <summary>
        /// Identifier for the template
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Human-readable name for UI / logs
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Base stats for units created from this template
        /// </summary>
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

