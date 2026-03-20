namespace Setup.Loading;

using Setup.Config;
using Setup.Validation.Primitives;

public sealed class ContentPack : IContentPackBuilder
{
    private ContentPackTemplates _templates = new ContentPackTemplates();
    private ValidationCollector _issueCollector = new ValidationCollector();
    private List<GameStateConfig> _gameStates= new List<GameStateConfig>();

    public ContentPackTemplates Templates => _templates;
    public bool HasErrors =>
    IssueView.Issues.Any(i => i.Severity == ContentIssueSeverity.Error);

    public IReadOnlyList<GameStateConfig> GameStates => _gameStates;

    public IContentIssueView IssueView => _issueCollector;

    IContentPackTemplatesBuilder IContentPackBuilder.ContentPackTemplatesBuilder => _templates;

    void IContentPackBuilder.AddIssues(ValidationCollector issues)
    {
        _issueCollector = issues;
    }

    void IContentPackBuilder.AddGameStates(List<GameStateConfig> gameStates)
    {
       _gameStates = gameStates;
    }

}
