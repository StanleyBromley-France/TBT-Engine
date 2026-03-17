using Agent.Tests.Agents.Mcts.Hashing.TestSupport;
using Core.Domain.Types;

namespace Agent.Tests.Agents.Mcts;

public sealed class GameStateHasherPhaseTests
{
    [Fact]
    public void Compute_CommittedPhaseChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateBaselineState();
        var changedState = state.DeepCloneForSimulation();
        changedState.Phase.MarkCommitted(new UnitInstanceId(1));

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }

    [Fact]
    public void Compute_CurrentlyCommitingChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateBaselineState();
        var changedState = state.DeepCloneForSimulation();
        changedState.Phase.SetCurrentlyCommiting(new UnitInstanceId(1));

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }
}
