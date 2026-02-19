using Core.Domain.Units.Instances.ReadOnly;

namespace Core.Domain.Units.Instances.Mutable;

/// <summary>
/// UnitDerivedStats represent computed, non-authoritative stat values for a unit.
/// These values are derived from UnitTemplate.BaseStats and the unit’s active
/// EffectInstances in GameState.
///
/// DerivedStats are not mutated directly by gameplay logic and store only
/// the result of a deterministic recomputation.
/// 
/// Exposed publicly only through IReadOnlyUnitDerivedStats.
/// </summary>
public class UnitDerivedStats : IReadOnlyUnitDerivedStats
{
    public int MovePoints { get; set; }
    public int ArmourPoints { get; set; }
    public int MagicResistance { get; set; }

    public int MaxHP { get; set; }
    public int MaxManaPoints { get; set; }
    public int ActionPoints { get; set; }

    public int HealingReceived { get; set; }
    public int HealingDealt { get; set; }
    public int DamageTaken { get; set; }
    public int DamageDealt { get; set; }

    public UnitDerivedStats(
        int movePoints,
        int armourPoints,
        int magicResistance,
        int maxHp,
        int maxManaPoints,
        int actionPoints,
        int healingReceived,
        int healingDealt,
        int damageTaken,
        int damageDealt)
    {
        MovePoints = movePoints;
        ArmourPoints = armourPoints;
        MagicResistance = magicResistance;

        MaxHP = maxHp;
        MaxManaPoints = maxManaPoints;
        ActionPoints = actionPoints;

        HealingReceived = healingReceived;
        HealingDealt = healingDealt;
        DamageTaken = damageTaken;
        DamageDealt = damageDealt;
    }
}
