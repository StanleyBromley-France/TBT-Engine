namespace Core.Engine.Rules;

using Core.Engine.Actions;
using Core.Engine.Mutation;
using Core.Game;
using Core.Domain.Types;
public interface IGameRules
{
    void ApplyAction(GameMutationContext context, ActionChoice action);

    IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state);

    bool IsActionLegal(IReadOnlyGameState state, ActionChoice action);

    TeamId? GetWinner(IReadOnlyGameState state);
}