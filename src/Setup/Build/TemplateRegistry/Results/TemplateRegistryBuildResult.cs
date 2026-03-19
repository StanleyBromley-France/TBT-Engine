namespace Setup.Build.TemplateRegistry.Results;

using Setup.Validation.Primitives;

using Core.Domain.Repositories;
public sealed class TemplateRegistryBuildResult
{
    public TemplateRegistry? TemplateRegistry { get; }
    public IContentIssueView IssueView { get; }

    public bool HasErrors =>
        IssueView.Issues.Any(i => i.Severity == ContentIssueSeverity.Error);

    public TemplateRegistryBuildResult(
        TemplateRegistry? templateRegistry,
        IContentIssueView issues)
    {
        TemplateRegistry = templateRegistry;
        IssueView = issues ?? throw new ArgumentNullException(nameof(issues));
    }
}
