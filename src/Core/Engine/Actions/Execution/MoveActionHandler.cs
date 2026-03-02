using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game;
using Core.Map.Pathfinding;

namespace Core.Engine.Actions.Execution;

public sealed class MoveActionHandler : IActionHandler<MoveAction>
{
    private readonly IPathfinder _pathfinder;

    public MoveActionHandler(IPathfinder pathfinder)
    {
        _pathfinder = pathfinder;
    }

    public void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        MoveAction action)
    {
        var unit = state.UnitInstances[action.UnitId];

        var cost = _pathfinder
            .GetMoveCost(state.Map, unit.Position, action.Target)!.Value;

        ctx.Movement.MoveUnit(action.UnitId, action.Target);
        ctx.Units.ChangeActionPoints(action.UnitId, -cost);
        ctx.Turn.CommitUnit(action.UnitId);
    }
}
