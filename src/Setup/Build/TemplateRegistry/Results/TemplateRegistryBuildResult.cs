namespace Setup.Build.TemplateRegistry.Results;

using Setup.Validation.Primitives;

using Core.Domain.Repositories;
public sealed class TemplateRegistryBuildResult
{
    public TemplateRegistry? TemplateRegistry { get; }
    public IReadOnlyList<ContentIssue> Issues { get; }

    public bool HasErrors =>
        Issues.Any(i => i.Severity == ContentIssueSeverity.Error);

    public TemplateRegistryBuildResult(
        TemplateRegistry? templateRegistry,
        IReadOnlyList<ContentIssue> issues)
    {
        TemplateRegistry = templateRegistry;
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
    }
}
