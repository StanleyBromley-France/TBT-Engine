namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Engine.Mutation;

/// <summary>
/// Mutation-layer API for modifying unit resource values>.
/// </summary>
/// <remarks>
/// Provides controlled mutation of unit resource fields such as HP, Mana,
/// and Action Points by updating entries in <see cref="Game.GameState.UnitInstances"/>.
/// </remarks>
public sealed class UnitsMutator
{
    private readonly IGameMutationAccess _ctx;

    public UnitsMutator(GameMutationContext ctx) => _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

    public void ChangeHp(UnitInstanceId unitId, int delta)
    {
        var state = _ctx.GetState();

        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.HP;
        unit.Resources.HP += delta;

        // TODO: Record undo step in UndoRecord
    }

    public void ChangeMana(UnitInstanceId unitId, int delta)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.Mana;
        unit.Resources.Mana += delta;

        // TODO: Record undo step in UndoRecord
    }

    public void ChangeActionPoints(UnitInstanceId unitId, int delta)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.ActionPoints;
        unit.Resources.ActionPoints += delta;

        // TODO: Record undo step in UndoRecord
    }

    // TODO: Add Effect change Mutator methods
}
