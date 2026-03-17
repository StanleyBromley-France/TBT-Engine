using Agent.Tests.Agents.Mcts.Hashing.TestSupport;

namespace Agent.Tests.Agents.Mcts;

public sealed class GameStateHasherRngTests
{
    [Fact]
    public void Compute_RngChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateBaselineState();
        var changedState = state.DeepCloneForSimulation();
        changedState.Rng = changedState.Rng.Advance();

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }
}
