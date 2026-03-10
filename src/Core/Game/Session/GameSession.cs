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
}