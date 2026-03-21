namespace Core.Game.Session;

using Core.Domain.Repositories;
using Core.Game.Match;

/// <summary>
/// Immutable match configuration shared for the lifetime of a match.
/// This data is safe to share between live and sandbox simulations.
/// </summary>
public sealed class GameContext
{
    public TemplateRegistry Content { get; }
    public TeamPair Teams { get; }
    internal GameSessionServices SessionServices { get; }
    internal GameContext(TemplateRegistry content, TeamPair teams, GameSessionServices sessionServices)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Teams = teams ?? throw new ArgumentNullException(nameof(teams));
        SessionServices = sessionServices ?? throw new ArgumentNullException(nameof(sessionServices));
    }
}