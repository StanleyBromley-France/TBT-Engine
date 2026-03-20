namespace Setup.Validation.Primitives;

public sealed class ValidationCollector : IContentIssueView
{
    private readonly List<ContentIssue> _issues = new();

    public bool HasErrors => _issues.Any(i => i.Severity == ContentIssueSeverity.Error);

    IReadOnlyList<ContentIssue> IContentIssueView.Issues => _issues;

    int IContentIssueView.Count => _issues.Count;

    public void Add(ContentIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        _issues.Add(issue);
    }

    public void AddRange(ValidationCollector issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        if (ReferenceEquals(this, issues)) throw new ArgumentException("Cannot add a collector to itself.", nameof(issues));
        
        _issues.AddRange(issues._issues);
    }

    public void AddError(string code, string message, string? path = null)
        => Add(new ContentIssue(code, message, path, ContentIssueSeverity.Error));

    public void AddWarning(string code, string message, string? path = null)
        => Add(new ContentIssue(code, message, path, ContentIssueSeverity.Warning));
}
