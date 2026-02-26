namespace Core.Engine.Rules;

using Core.Engine.Mutation;
using Core.Game;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Plans;

public interface IGameRules
{
    public ActionPlan BuildPlan(IReadOnlyGameState state, ActionChoice action);
    IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state);
    bool IsActionLegal(IReadOnlyGameState state, ActionChoice action);
    TeamId? GetWinner(IReadOnlyGameState state);
}