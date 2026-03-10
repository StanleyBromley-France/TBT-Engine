using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game.State.ReadOnly;

namespace Core.Engine.Actions.Execution;

public sealed class ChangeActiveUnitActionHandler
    : IActionHandler<ChangeActiveUnitAction>
{
    public void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        ChangeActiveUnitAction action)
    {
        ctx.Turn.ChangeActiveUnit(action.NewActiveUnitId);
    }
}
