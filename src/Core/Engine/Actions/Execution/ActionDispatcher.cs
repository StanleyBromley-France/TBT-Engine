namespace Core.Engine.Actions.Execution;

using Core.Engine.Actions.Choice;
using Core.Engine.Mutation;
using Core.Game.State.ReadOnly;

public sealed class ActionDispatcher : IActionDispatcher
{
    private readonly Dictionary<Type, Action<IReadOnlyGameState, GameMutationContext, ActionChoice>> _handlers
        = new();

    public void Register<TAction>(IActionHandler<TAction> handler)
        where TAction : ActionChoice
    {
        _handlers[typeof(TAction)] = (state, ctx, action) =>
            handler.Execute(state, ctx, (TAction)action);
    }

    public void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        ActionChoice action)
    {
        if (!_handlers.TryGetValue(action.GetType(), out var executor))
            throw new InvalidOperationException(
                $"No handler registered for {action.GetType().Name}");

        executor(state, ctx, action);
    }
}