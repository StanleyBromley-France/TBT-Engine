namespace Core.Engine.Actions.Execution;

using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game.State.ReadOnly;

public sealed class SkipActiveUnitHandler : IActionHandler<SkipActiveUnitAction>
{
    public void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        SkipActiveUnitAction action)
    {
        var unit = state.UnitInstances[action.UnitId];

        // Set AP to 0
        if (unit.Resources.ActionPoints != 0)
            ctx.Units.ChangeActionPoints(unit.Id, -unit.Resources.ActionPoints);
    }
}
