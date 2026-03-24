namespace Setup.Loading.Scenarios;

using Setup.Loading;
using Setup.Validation.Primitives;

public sealed class LoadedScenarioContent
{
    public ContentPack ContentPack { get; }

    public IReadOnlyList<string> GameStateIds { get; }

    public IContentIssueView IssueView { get; }

    public bool HasErrors => IssueView.Issues.Any(issue => issue.Severity == ContentIssueSeverity.Error);

    public LoadedScenarioContent(
        ContentPack contentPack,
        IContentIssueView issueView)
    {
        ContentPack = contentPack ?? throw new ArgumentNullException(nameof(contentPack));
        IssueView = issueView ?? throw new ArgumentNullException(nameof(issueView));
        GameStateIds = ContentPack.GameStates.Select(gameState => gameState.Id).ToArray();
    }
}
