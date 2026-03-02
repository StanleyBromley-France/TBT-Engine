namespace Core.Game;

using Core.Domain.Repositories;
using Core.Engine.Undo;
using Core.Game.Match;

/// <summary>
/// Owns the runtime context for a single match: compiled static content
/// (<see cref="TemplateRegistry"/>) and the current mutable <see cref="GameState"/>.
/// </summary>
public sealed class GameSession
{
    public TemplateRegistry Content { get; }
    public GameState State { get; }
    public TeamPair Teams { get; }
    public UndoHistory Undo { get; }
    public GameSession(TemplateRegistry content, GameState initialState, TeamPair teams, UndoHistory history)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        State = initialState ?? throw new ArgumentNullException(nameof(initialState));
        Teams = teams ?? throw new ArgumentNullException(nameof(teams));
        Undo = history ?? throw new ArgumentNullException(nameof(history));
    }
}
