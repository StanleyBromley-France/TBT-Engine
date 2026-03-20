namespace Setup.Validation.Primitives;

public interface IContentIssueView
{
    public IReadOnlyList<ContentIssue> Issues { get; }
    public int Count { get; }
}
