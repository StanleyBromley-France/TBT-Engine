using Agent.Tests.Agents.Mcts.Hashing.TestSupport;
using Core.Domain.Types;

namespace Agent.Tests.Agents.Mcts.Hashing;

public sealed class GameStateHasherTurnStateTests
{
    [Fact]
    public void Compute_TeamToActChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateStateWithTwoAllies();
        var changedState = state.DeepCloneForSimulation();
        changedState.Turn = new Turn(changedState.Turn.AttackerTurnsTaken, new TeamId(2));

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }

    [Fact]
    public void Compute_AttackerTurnsTakenChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateBaselineState();
        var changedState = state.DeepCloneForSimulation();
        changedState.Turn = new Turn(changedState.Turn.AttackerTurnsTaken + 1, changedState.Turn.TeamToAct);

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }
}
