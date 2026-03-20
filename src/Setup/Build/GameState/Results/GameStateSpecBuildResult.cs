namespace Setup.Build.GameState.Results;

using Core.Game.Bootstrap.Contracts;
using Setup.Validation.Primitives;

public sealed class GameStateSpecBuildResult
{
    public IGameStateSpec? Spec { get; }
    public IContentIssueView IssueView { get; }

    public bool HasErrors =>
        IssueView.Issues.Any(i => i.Severity == ContentIssueSeverity.Error);

    public GameStateSpecBuildResult(
        IGameStateSpec? spec,
        IContentIssueView issues)
    {
        Spec = spec;
        IssueView = issues ?? throw new ArgumentNullException(nameof(issues));
    }
}
