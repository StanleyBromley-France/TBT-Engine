using Agent.Tests.Agents.Mcts.Hashing.TestSupport;
using Core.Domain.Types;
using Core.Map.Grid;
using Core.Map.Terrain;

namespace Agent.Tests.Agents.Mcts.Hashing;

public sealed class GameStateHasherMapTests
{
    [Fact]
    public void Compute_MapTerrainChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateBaselineState();
        var changedState = state.DeepCloneForSimulation();
        var tile = (Tile?)changedState.Map.GetTile(new HexCoord(0, 0));

        Assert.NotNull(tile);
        tile!.Terrain = TerrainType.Water;

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }
}
