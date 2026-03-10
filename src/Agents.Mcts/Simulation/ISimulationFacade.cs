namespace Agents.Mcts.Simulation;

using Core.Engine.Actions.Choice;
using Core.Game.State.ReadOnly;
using Core.Undo;

public interface ISimulationFacade
{
    IReadOnlyGameState GetState();
    IReadOnlyList<ActionChoice> GetLegalActions();
    UndoMarker MarkUndo();
    void ApplyAction(ActionChoice action);
    void UndoTo(UndoMarker marker);
    bool IsTerminal();
}
