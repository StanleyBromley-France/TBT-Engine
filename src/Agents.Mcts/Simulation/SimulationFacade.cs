namespace Agents.Mcts.Simulation;

using Core.Engine;
using Core.Engine.Actions.Choice;
using Core.Game.Match;
using Core.Game.State.ReadOnly;
using Core.Undo;

public sealed class SimulationFacade : ISimulationFacade
{
    private readonly EngineFacade _engine;

    public SimulationFacade(EngineFacade engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public IReadOnlyGameState GetState() => _engine.GetState();

    public GameOutcome GetOutcome() => _engine.GetOutcome();

    public IReadOnlyList<ActionChoice> GetLegalActions() => _engine.GetLegalActions().ToList();

    public UndoMarker MarkUndo() => _engine.MarkUndo();

    public void ApplyAction(ActionChoice action) => _engine.ApplyAction(action);

    public void UndoTo(UndoMarker marker) => _engine.UndoTo(marker);

    public bool IsTerminal() => _engine.IsGameOver();
}
