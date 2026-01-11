using Core.Units.Templates;

namespace Core.Units.Instances;

using Core.Types;
/// <summary>
/// Represents a single runtime instance of a unit on the battlefield
/// </summary>
/// <remarks>
/// <para>
/// Stores all temporary, changing values associated with a unit during a match
/// while referencing a <see cref="UnitTemplate"/> for its base data
/// </para>
/// </remarks>
public class UnitInstance
{
    public UnitInstanceId Id { get; set; }

    public Team Team { get; set; }

    public UnitTemplate Template { get; set; }

    public int CurrentHP { get; set; }

    public int CurrentActionPoints { get; set; }

    public int CurrentManaPoints { get; set; }

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

    public UnitInstance(UnitInstanceId id, Team team, UnitTemplate template, Position startPosition)
    {
        Id = id;
        Team = team;
        Template = template;
        Position = startPosition;

        CurrentHP = template.BaseStats.MaxHP;
        CurrentActionPoints = template.BaseStats.DefaultActionPoints;
        CurrentManaPoints = template.BaseStats.MaxManaPoints;
    }
}

