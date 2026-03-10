namespace Core.Domain.Units.Instances.Mutable;

using Core.Domain.Types;
using Core.Domain.Units.Instances.ReadOnly;
using Core.Domain.Units.Templates;

/// <summary>
/// Represents a single runtime instance of a unit on the battlefield.
/// </summary>
/// <remarks>
/// <para>
/// Stores all temporary, changing values associated with a unit during a match
/// while referencing a <see cref="UnitTemplate"/> for its base data.
/// </para>
/// <para>
/// This type is exposed publicly only through its read-only interface.
/// Direct mutation is restricted to GameMutationContext, which operates on the
/// concrete UnitInstance and its mutable subcomponents.
/// </para>
/// </remarks>

public class UnitInstance : IReadOnlyUnitInstance
{
    public UnitInstanceId Id { get; }
    public TeamId Team { get; }
    public UnitTemplate Template { get; }

    public UnitResources Resources { get; }
    public UnitDerivedStats DerivedStats { get; set; }

    public HexCoord Position { get; internal set; }

    public bool IsAlive => Resources.HP > 0;

    // Interface projection
    IReadOnlyUnitResources IReadOnlyUnitInstance.Resources => Resources;
    IReadOnlyUnitDerivedStats IReadOnlyUnitInstance.DerivedStats => DerivedStats;

    public UnitInstance(
        UnitInstanceId id,
        TeamId team,
        UnitTemplate template,
        HexCoord position)
    {
        Id = id;
        Team = team;
        Template = template;
        Position = position;

        Resources = new UnitResources(
            template.BaseStats.MaxHP,
            template.BaseStats.MovePoints,
            template.BaseStats.ActionPoints,
            template.BaseStats.MaxManaPoints
        );

        DerivedStats = new UnitDerivedStats(
            template.BaseStats.MovePoints,
            template.BaseStats.PhysicalDamageReceived,
            template.BaseStats.MagicDamageReceived,
            template.BaseStats.MaxHP,
            template.BaseStats.MaxManaPoints,
            template.BaseStats.ActionPoints,
            template.BaseStats.HealingReceived,
            template.BaseStats.HealingDealt,
            template.BaseStats.DamageDealt
        );

    }

    public UnitInstance DeepCloneForSimulation()
    {
        var clone = new UnitInstance(Id, Team, Template, Position);

        clone.Resources.HP = Resources.HP;
        clone.Resources.MovePoints = Resources.MovePoints;
        clone.Resources.ActionPoints = Resources.ActionPoints;
        clone.Resources.Mana = Resources.Mana;
        clone.DerivedStats = DerivedStats.DeepCloneForSimulation();

        return clone;
    }
}
