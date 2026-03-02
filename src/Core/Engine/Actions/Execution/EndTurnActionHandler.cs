namespace Core.Engine.Actions.Execution;

using Core.Engine.Actions.Choice;
using Core.Game;
using Core.Engine.Mutation;

public sealed class EndTurnActionHandler : IActionHandler<SkipActiveUnit>
{
    public void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        SkipActiveUnit action)
    {
        var unit = state.UnitInstances[action.UnitId];

        // Set AP to 0
        if (unit.Resources.ActionPoints != 0)
            ctx.Units.ChangeActionPoints(unit.Id, -unit.Resources.ActionPoints);

        // Commit if not already committed
        if (!state.Phase.CommittedThisPhase.Contains(unit.Id))
            ctx.Turn.CommitUnit(unit.Id);
    }
}