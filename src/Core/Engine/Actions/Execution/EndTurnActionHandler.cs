using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game;

namespace Core.Engine.Actions.Execution;

public sealed class EndTurnActionHandler : IActionHandler<EndTurnAction>
{
    public void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        EndTurnAction action)
    {
        var team = state.Turn.TeamToAct;

        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive)
                continue;

            if (unit.Team != team)
                continue;

            if (state.Phase.CommittedThisPhase.Contains(unit.Id))
                continue;

            ctx.Turn.CommitUnit(unit.Id);
        }
    }
}