namespace Setup.Validation.Primitives;

public sealed class ContentIssue
{
    public string Code { get; }
    public string Message { get; }
    public string? Path { get; }
    public ContentIssueSeverity Severity { get; }

    public ContentIssue(string code, string message, string? path, ContentIssueSeverity severity)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Path = path;
        Severity = severity;
    }
}
