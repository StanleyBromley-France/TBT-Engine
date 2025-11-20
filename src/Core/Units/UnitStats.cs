namespace Core.Units
{
    /// <summary>
    /// Defines the static base statistics for a unit type
    /// </summary>
    public class UnitStats
    {
        /// <summary>
        /// Maximum hit points
        /// </summary>
        public int MaxHP { get; set; }

        /// <summary>
        /// Maximum mana points
        /// </summary>
        public int MaxManaPoints { get; set; }

        /// <summary>
        /// How far a unit can move (in tiles) when choosing movement action
        /// </summary>
        public int MovePoints { get; set; }

        /// <summary>
        /// Maximum action points per turn
        /// </summary>
        public int DefaultActionPoints { get; set; } = 2;

        /// <summary>
        /// Armour points for this unit
        /// </summary>
        public int ArmourPoints { get; set; }

        public UnitStats()
        {
        }

        public UnitStats(int maxHp, int movePoints, int armourPoints)
        {
            MaxHP = maxHp;
            MovePoints = movePoints;
            ArmourPoints = armourPoints;
        }
    }

}
