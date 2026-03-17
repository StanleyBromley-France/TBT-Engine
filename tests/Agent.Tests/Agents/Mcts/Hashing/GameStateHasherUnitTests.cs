using Agent.Tests.Agents.Mcts.Hashing.TestSupport;
using Agent.Tests.Engine.TestSupport;
using Core.Domain.Types;

namespace Agent.Tests.Agents.Mcts.Hashing;

public sealed class GameStateHasherUnitTests
{
    [Fact]
    public void Compute_UnitHpChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateBaselineState();
        var changedState = state.DeepCloneForSimulation();
        changedState.UnitInstances[new UnitInstanceId(1)].Resources.HP -= 1;

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }

    [Fact]
    public void Compute_UnitPositionChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var originalAttacker = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(0, 0));
        var changedAttacker = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(0, 1));
        var defender = EngineTestFactory.CreateUnit(2, team: 2, position: new HexCoord(1, 0));

        var state = EngineTestFactory.CreateState(
            new[] { originalAttacker, defender.DeepCloneForSimulation() },
            teamToAct: 1,
            attackerTurnsTaken: 2);

        var changedState = EngineTestFactory.CreateState(
            new[] { changedAttacker, defender.DeepCloneForSimulation() },
            teamToAct: 1,
            attackerTurnsTaken: 2);

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }
}

