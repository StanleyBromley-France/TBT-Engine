namespace Setup.Build.Scenarios;

using Core.Domain.Repositories;
using Core.Game.Bootstrap.Contracts;
using Setup.Validation.Primitives;

public sealed class ScenarioResult
{
    public TemplateRegistry? TemplateRegistry { get; }

    public IGameStateSpec? GameStateSpec { get; }

    public IContentIssueView IssueView { get; }

    public bool HasErrors => IssueView.Issues.Any(issue => issue.Severity == ContentIssueSeverity.Error);

    public ScenarioResult(
        TemplateRegistry? templateRegistry,
        IGameStateSpec? gameStateSpec,
        IContentIssueView issueView)
    {
        TemplateRegistry = templateRegistry;
        GameStateSpec = gameStateSpec;
        IssueView = issueView ?? throw new ArgumentNullException(nameof(issueView));
    }
}
