namespace Setup.Build.Scenarios;

using Core.Domain.Repositories;
using Setup.Build.GameState;
using Setup.Build.GameState.Map;
using Setup.Build.GameState.Unit;
using Setup.Loading;
using Setup.Validation.Primitives;

internal sealed class ScenarioSource : IScenarioSource
{
    private readonly ContentPack _contentPack;
    private readonly TemplateRegistry? _templateRegistry;

    public IReadOnlyList<string> GameStateIds { get; }

    public IContentIssueView IssueView { get; }

    public bool HasErrors => IssueView.Issues.Any(issue => issue.Severity == ContentIssueSeverity.Error);

    public ScenarioSource(
        ContentPack contentPack,
        TemplateRegistry? templateRegistry,
        IContentIssueView issueView)
    {
        _contentPack = contentPack ?? throw new ArgumentNullException(nameof(contentPack));
        _templateRegistry = templateRegistry;
        IssueView = issueView ?? throw new ArgumentNullException(nameof(issueView));
        GameStateIds = _contentPack.GameStates.Select(gameState => gameState.Id).ToArray();
    }

    public ScenarioResult Build(
        string gameStateId,
        ContentValidationMode validationMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameStateId);

        if (_templateRegistry is null)
            return new ScenarioResult(null, null, IssueView);

        var specBuilder = new GameStateSpecBuilder(new MapSpecBuilder(), new UnitSpawnSpecBuilder());
        var specResult = specBuilder.Build(_contentPack, _templateRegistry, gameStateId, validationMode);
        if (specResult.Spec is null)
            return new ScenarioResult(_templateRegistry, null, specResult.IssueView);

        var issues = new ValidationCollector();
        CopyIssues(IssueView, issues);
        CopyIssues(specResult.IssueView, issues);

        return new ScenarioResult(_templateRegistry, specResult.Spec, issues);
    }

    private static void CopyIssues(IContentIssueView source, ValidationCollector destination)
    {
        foreach (var issue in source.Issues)
            destination.Add(issue);
    }
}
