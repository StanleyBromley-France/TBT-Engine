using Core.Domain.Units.Instances.ReadOnly;

namespace Core.Domain.Units.Instances.Mutable;

/// <summary> /// 
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
    // Core resources
    public int MaxHP { get; set; }
    public int MaxManaPoints { get; set; }
    public int MaxMovePoints { get; set; }
    public int MaxActionPoints { get; set; }

    // Percentage modifiers (100 = normal effectiveness)

    // Outgoing
    public int DamageDealt { get; set; }
    public int HealingDealt { get; set; }

    // Incoming
    public int HealingReceived { get; set; }

    // Damage-type specific
    public int PhysicalDamageReceived { get; set; }
    public int MagicDamageReceived { get; set; }

    public UnitDerivedStats(
        int movePoints,
        int physicalDamageModifier,
        int magicDamageModifier,
        int maxHp,
        int maxManaPoints,
        int actionPoints,
        int healingReceived,
        int healingDealt,
        int damageDealt)
    {
        MaxHP = maxHp;
        MaxManaPoints = maxManaPoints;

        MaxMovePoints = movePoints;
        MaxActionPoints = actionPoints;

        DamageDealt = damageDealt;
        HealingDealt = healingDealt;
        HealingReceived = healingReceived;

        PhysicalDamageReceived = physicalDamageModifier;
        MagicDamageReceived = magicDamageModifier;
    }
}