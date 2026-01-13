namespace Core.Game;

using Core.Domain.Repositories;

/// <summary>
/// Owns the runtime context for a single match: compiled static content
/// (<see cref="TemplateRegistry"/>) and the current mutable <see cref="GameState"/>.
/// </summary>
public sealed class GameSession
{
    public TemplateRegistry Content { get; }
    public GameState State { get; }
    public DeterministicRng Rng { get; }
    public GameSession(TemplateRegistry content, GameState initialState)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        State = initialState ?? throw new ArgumentNullException(nameof(initialState));
        Rng = new DeterministicRng();
    }
}
