namespace Core.Engine.Undo;

public readonly struct UndoMarker
{
    public int RecordCount { get; }

    public UndoMarker(int recordCount)
    {
        if (recordCount < 0)
            throw new ArgumentOutOfRangeException(nameof(recordCount));

        RecordCount = recordCount;
    }

    public override string ToString() => $"UndoMarker(RecordCount={RecordCount})";
}