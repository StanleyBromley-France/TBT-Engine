namespace Core.Engine.Undo;

using Core.Game;

public sealed class UndoHistory
{
    private readonly List<UndoRecord> _records = new();

    public IReadOnlyList<UndoRecord> Records => _records;

    public bool CanUndo => _records.Count > 0;

    public UndoMarker Mark() => new(_records.Count);

    public void Commit(UndoRecord record)
    {
        if (record is null)
            throw new ArgumentNullException(nameof(record));

        _records.Add(record);
    }

    public void UndoLast(GameState state)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        if (_records.Count == 0)
            return;

        var lastIndex = _records.Count - 1;
        var record = _records[lastIndex];

        _records.RemoveAt(lastIndex);
        record.UndoAll(state);
    }

    public void UndoTo(GameState state, UndoMarker marker)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        if (marker.RecordCount < 0 || marker.RecordCount > _records.Count)
            throw new ArgumentOutOfRangeException(nameof(marker));

        while (_records.Count > marker.RecordCount)
        {
            UndoLast(state);
        }
    }

    public void Clear()
    {
        _records.Clear();
    }
}