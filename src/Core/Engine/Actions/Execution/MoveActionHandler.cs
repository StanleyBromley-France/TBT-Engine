using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game.State.ReadOnly;
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

        var cost = _pathfinder.GetMoveCost(state.Map, unit.Position, action.TargetHex)!.Value;

        ctx.Movement.MoveUnit(action.UnitId, action.TargetHex);
        ctx.Units.ChangeMovePoints(action.UnitId, -cost);
        ctx.Units.ChangeActionPoints(action.UnitId, -1);
        ctx.Turn.CommitUnit(action.UnitId);
    }
}
