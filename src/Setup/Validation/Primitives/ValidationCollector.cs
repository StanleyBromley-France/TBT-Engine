namespace Setup.Validation.Primitives;

public sealed class ValidationCollector
{
    private readonly List<ContentIssue> _issues = new();
    private readonly IReadOnlyList<ContentIssue> _readOnlyIssues;

    public ValidationCollector()
    {
        _readOnlyIssues = _issues.AsReadOnly();
    }

    public IReadOnlyList<ContentIssue> Issues => _readOnlyIssues;

    public int Count => _issues.Count;

    public bool HasErrors => _issues.Any(i => i.Severity == ContentIssueSeverity.Error);

    public bool ShouldHalt(ContentValidationMode mode)
        => mode == ContentValidationMode.Strict && HasErrors;

    public void Add(ContentIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        _issues.Add(issue);
    }

    public void AddError(string code, string message, string? path = null)
        => Add(new ContentIssue(code, message, path, ContentIssueSeverity.Error));

    public void AddWarning(string code, string message, string? path = null)
        => Add(new ContentIssue(code, message, path, ContentIssueSeverity.Warning));
}
