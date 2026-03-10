using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game.State.ReadOnly;

namespace Core.Engine.Actions.Execution;

public interface IActionDispatcher
{
    void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        ActionChoice action);
}
