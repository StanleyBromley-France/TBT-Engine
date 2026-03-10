namespace Core.Undo;

using Core.Game;
using Core.Undo.Steps;

public sealed class UndoRecord
{
    private readonly List<IUndoStep> _steps = new();

    public IReadOnlyList<IUndoStep> Steps => _steps;

    public bool IsEmpty => _steps.Count == 0;

    public void AddStep(IUndoStep step)
    {
        if (step is null) throw new ArgumentNullException(nameof(step));
        _steps.Add(step);
    }

    /// <summary>
    /// Replays all steps in reverse order against the provided state.
    /// </summary>
    public void UndoAll(GameState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        for (int i = _steps.Count - 1; i >= 0; i--)
            _steps[i].Undo(state);
    }
}