namespace Core.Units
{
    /// <summary>
    /// Defines the static base statistics for a unit type
    /// </summary>
    public class UnitStats
    {
        public int MaxHP { get; set; }

        public int MaxManaPoints { get; set; }

        public int MovePoints { get; set; }

        public int DefaultActionPoints { get; set; } = 2;

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
