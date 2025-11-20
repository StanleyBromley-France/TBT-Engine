namespace Core.Units
{
    /// <summary>
    /// Represents a single runtime instance of a unit on the battlefield
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stores all temporary, changing values associated with a unit during a match
    /// while referencing a <see cref="UnitTemplate"/> for its base data
    /// </para>
    /// </remarks>
    public class Unit
    {
        /// <summary>
        /// Unique identifier for this specific unit instance
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Which team this unit belongs to (Attacker / Defender)
        /// </summary>
        public Team Team { get; set; }

        /// <summary>
        /// Static template describing this unit's base stats and identity
        /// </summary>
        public UnitTemplate Template { get; set; }

        /// <summary>
        /// Current hit points
        /// </summary>
        public int CurrentHP { get; set; }

        /// <summary>
        /// Current number of available action points for the unit this turn
        /// </summary>
        public int CurrentActionPoints { get; set; }

        /// <summary>
        /// Current number of available mana points for the unit this turn
        /// </summary>
        public int CurrentManaPoints { get; set; }

        /// <summary>
        /// Coordinates on the map grid
        /// </summary>
        /// 
        public Position Position { get; set; }

        /// <summary>
        /// Restores this unit's action points to its default value
        /// Should be called at the start of a teams turn
        /// </summary>
        public void ResetActionPoints()
        {
            CurrentActionPoints = Template.BaseStats.DefaultActionPoints;
        }

        /// <summary>
        /// Is this unit still alive?
        /// </summary>
        public bool IsAlive
        {
            get { return CurrentHP > 0; }
        }

        public Unit()
        {
            Id = Guid.NewGuid().ToString("N");
            Template = new UnitTemplate();
            Position = new Position(0, 0);
            CurrentHP = 0;
            CurrentActionPoints = 0;
        }

        public Unit(Team team, UnitTemplate template, Position startPosition)
        {
            Id = Guid.NewGuid().ToString("N");
            Team = team;
            Template = template;
            Position = startPosition;

            CurrentHP = template.BaseStats.MaxHP;
            CurrentActionPoints = template.BaseStats.DefaultActionPoints;
            CurrentManaPoints = template.BaseStats.MaxManaPoints;
        }

        // TODO: Add abilities and active effects
    }
}
