namespace Setup.Build.GameState.Results;

using Core.Game.Bootstrap.Contracts;
using Setup.Validation.Primitives;

public sealed class MapSpecBuildResult
{
    public IMapSpec? MapSpec { get; }
    public IContentIssueView IssueView { get; }

    public bool HasErrors =>
        IssueView.Issues.Any(i => i.Severity == ContentIssueSeverity.Error);

    public MapSpecBuildResult(
        IMapSpec? mapSpec,
        IContentIssueView issues)
    {
        MapSpec = mapSpec;
        IssueView = issues ?? throw new ArgumentNullException(nameof(issues));
    }
}
