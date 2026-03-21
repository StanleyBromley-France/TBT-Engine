namespace Core.Game.Session;

using Core.Game.Match;
using Core.Game.State;
using Core.Undo;

/// <summary>
/// Mutable runtime state for a match. This contains all simulation data
/// that evolves as the game progresses and must be cloned for sandbox
/// simulations (e.g. MCTS).
/// </summary>
public sealed class GameRuntime
{
    public GameState State { get; }
    public UndoHistory Undo { get; }
    public GameOutcome Outcome { get; private set; }
    public InstanceAllocationState InstanceAllocation { get; private set; }
    public GameRuntime(GameState state, UndoHistory undo, GameOutcome outcome, InstanceAllocationState instanceAllocation)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Undo = undo ?? throw new ArgumentNullException(nameof(undo));
        Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        InstanceAllocation = instanceAllocation ?? throw new ArgumentNullException(nameof(instanceAllocation));
    }

    public void SetGameOutcome(GameOutcome outcome)
    {
        if (outcome == null)
            throw new ArgumentNullException(nameof(outcome));

        Outcome = outcome;
    }

    /// <summary>
    /// Deep-clones mutable runtime simulation data for sandbox use.
    /// Undo history always starts empty in the sandbox.
    /// </summary>
    public GameRuntime DeepCloneForSimulation()
    {
        return new GameRuntime(
            state: State.DeepCloneForSimulation(),
            undo: new UndoHistory(),
            outcome: Outcome,
            instanceAllocation: InstanceAllocation.DeepCloneForSimulation());
    }
}
