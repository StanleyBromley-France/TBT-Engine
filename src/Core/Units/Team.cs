namespace Core.Units
{
    /// <summary>
    /// Identifies which side a unit belongs to in a match
    /// Used for turn sequencing, targeting rules, and win conditions
    /// </summary>
    public enum Team
    {
        Attacker = 0,
        Defender = 1
    }
}
