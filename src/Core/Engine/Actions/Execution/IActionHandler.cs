using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game;

namespace Core.Engine.Actions.Execution;


public interface IActionHandler<in TAction> where TAction : ActionChoice
{
    void Execute(IReadOnlyGameState state, GameMutationContext ctx, TAction action);
}