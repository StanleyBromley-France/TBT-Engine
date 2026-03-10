namespace Core.Game.Session;

/// <summary>
/// Root object representing a match instance.
/// Combines immutable match context with mutable runtime state.
/// </summary>
public sealed class GameSession
{
    public GameContext Context { get; }
    public GameRuntime Runtime { get; }

    public GameSession(GameContext context, GameRuntime runtime)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// Creates an isolated simulation session that shares immutable match context
    /// but deep-clones mutable runtime state.
    /// </summary>
    public GameSession CreateSandbox()
    {
        return new GameSession(Context, Runtime.DeepCloneForSimulation());
    }
}
