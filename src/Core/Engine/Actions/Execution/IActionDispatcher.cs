using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game;

namespace Core.Engine.Actions.Execution;

public interface IActionDispatcher
{
    void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        ActionChoice action);
}
