namespace Agents.Mcts.Config;

public sealed class MctsAgentProfile
{
    public string Name { get; set; } = null!;
    /// <summary>
    /// Value of keeping allied units alive.
    /// </summary>
    public double AllyUnitWeight { get; set; }
    /// <summary>
    /// Value of removing enemy units.
    /// </summary>
    public double EnemyUnitWeight { get; set; }
    /// <summary>
    /// Value of preserving allied HP.
    /// </summary>
    public double AllyHpWeight { get; set; }
    /// <summary>
    /// Value of reducing enemy HP.
    /// </summary>
    public double EnemyHpWeight { get; set; }
    /// <summary>
    /// Value of increasing allied resource capacity through derived stats.
    /// This captures effects that raise maxima such as HP, mana, move points, or action points.
    /// </summary>
    public double AllyResourceCapacityWeight { get; set; }
    /// <summary>
    /// Value of reducing enemy resource capacity through derived stats.
    /// This captures effects that shrink maxima such as HP, mana, move points, or action points.
    /// </summary>
    public double EnemyResourceCapacityWeight { get; set; }
    /// <summary>
    /// Value of improving allied derived combat modifiers relative to the unit's base template stats.
    /// This captures buffs and debuffs that affect damage and healing effectiveness or damage taken.
    /// </summary>
    public double AllyDerivedCombatWeight { get; set; }
    /// <summary>
    /// Value of worsening enemy derived combat modifiers relative to the unit's base template stats.
    /// This captures buffs and debuffs that affect damage and healing effectiveness or damage taken.
    /// </summary>
    public double EnemyDerivedCombatWeight { get; set; }
    /// <summary>
    /// Value of preserving allied mana for future actions.
    /// </summary>
    public double AllyManaWeight { get; set; }
    /// <summary>
    /// Value of draining enemy mana reserves.
    /// </summary>
    public double EnemyManaWeight { get; set; }
    /// <summary>
    /// Weight applied to the number of attacker turns still remaining.
    /// </summary>
    public double RemainingAttackerTurnsWeight { get; set; }
    /// <summary>
    /// Weight applied to the number of attacker turns already spent.
    /// </summary>
    public double ElapsedAttackerTurnsWeight { get; set; }
    /// <summary>
    /// Large terminal-state bonus or penalty so wins dominate heuristic terms.
    /// </summary>
    public double WinScore { get; set; }

    public static MctsAgentProfile Balanced(string name = "Balanced")
    {
        return new MctsAgentProfile
        {
            Name = name,
            AllyUnitWeight = 100d,
            EnemyUnitWeight = 100d,
            AllyHpWeight = 1d,
            EnemyHpWeight = 1d,
            AllyResourceCapacityWeight = 20d,
            EnemyResourceCapacityWeight = 20d,
            AllyDerivedCombatWeight = 25d,
            EnemyDerivedCombatWeight = 25d,
            AllyManaWeight = 1d,
            EnemyManaWeight = 1d,
            RemainingAttackerTurnsWeight = 0d,
            ElapsedAttackerTurnsWeight = 0d,
            WinScore = 1_000_000d
        };
    }

    public static MctsAgentProfile Offensive(string name = "Offensive")
    {
        return new MctsAgentProfile
        {
            Name = name,
            AllyUnitWeight = 95d,
            EnemyUnitWeight = 140d,
            AllyHpWeight = 0.8d,
            EnemyHpWeight = 1.35d,
            AllyResourceCapacityWeight = 16d,
            EnemyResourceCapacityWeight = 24d,
            AllyDerivedCombatWeight = 22d,
            EnemyDerivedCombatWeight = 32d,
            AllyManaWeight = 0.75d,
            EnemyManaWeight = 1.1d,
            RemainingAttackerTurnsWeight = 35d,
            ElapsedAttackerTurnsWeight = 0d,
            WinScore = 1_000_000d,
        };
    }

    public static MctsAgentProfile Defensive(string name = "Defensive")
    {
        return new MctsAgentProfile
        {
            Name = name,
            AllyUnitWeight = 140d,
            EnemyUnitWeight = 95d,
            AllyHpWeight = 1.35d,
            EnemyHpWeight = 0.8d,
            AllyResourceCapacityWeight = 24d,
            EnemyResourceCapacityWeight = 16d,
            AllyDerivedCombatWeight = 32d,
            EnemyDerivedCombatWeight = 22d,
            AllyManaWeight = 1.1d,
            EnemyManaWeight = 0.75d,
            RemainingAttackerTurnsWeight = 0d,
            ElapsedAttackerTurnsWeight = 35d,
            WinScore = 1_000_000d,
        };
    }
}
