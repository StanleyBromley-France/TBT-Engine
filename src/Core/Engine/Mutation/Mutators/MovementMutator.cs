namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Engine.Mutation;
using Core.Engine.Undo.Steps.Move;

public sealed class MovementMutator : IMovementMutator
{
    private readonly IGameMutationAccess _ctx;

    public MovementMutator(GameMutationContext ctx)
        => _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

    public void MoveUnit(UnitInstanceId unitId, HexCoord newPos)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Position;

        state.OccupiedHexes.Remove(before);
        state.OccupiedHexes.Add(newPos);

        unit.Position = newPos;

        _ctx.GetUndo().AddStep(new UnitPositionChangeUndo(unitId, before));
    }
}
