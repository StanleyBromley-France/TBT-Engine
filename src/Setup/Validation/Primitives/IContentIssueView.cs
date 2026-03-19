namespace Setup.Validation.Primitives;

public interface IContentIssueView
{
    public IReadOnlyList<ContentIssue> Issues { get; }
    public int Count { get; }
    bool HasErrors { get; }

    public bool ShouldHalt(ContentValidationMode mode);
}
