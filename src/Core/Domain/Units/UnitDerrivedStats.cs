namespace Core.Domain.Units;

/// <summary>
/// UnitDerivedStats represent computed, non-authoritative stat values for a unit.
/// These values are derived from UnitTemplate.BaseStats and the unit’s active
/// EffectInstances in GameState.
///
/// DerivedStats are not mutated directly by gameplay logic and store only
/// the result of a deterministic recomputation.
/// 
/// Exposed publicly only through IReadOnlyUnitResources.
/// </summary>
public class UnitDerivedStats : IReadOnlyUnitDerivedStats
{
    public int MagicResistance { get; set; }
    public int Armor { get; set; }

    public UnitDerivedStats(int magicResistance, int armor)
    {
        MagicResistance = magicResistance;
        Armor = armor;
    }
}