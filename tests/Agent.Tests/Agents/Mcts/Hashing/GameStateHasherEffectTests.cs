using Agent.Tests.Agents.Mcts.Hashing.TestSupport;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Types;

namespace Agent.Tests.Agents.Mcts.Hashing;

public sealed class GameStateHasherEffectTests
{
    [Fact]
    public void Compute_EffectResolvedRuntimeStateChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateStateWithEffect();
        var changedState = state.DeepCloneForSimulation();
        var changedEffect = changedState.ActiveEffects[new UnitInstanceId(2)][new EffectInstanceId(10)];
        ((IResolvableHpDeltaComponent)changedEffect.Components[0]).ResolvedHpDelta = 7;

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }

    [Fact]
    public void Compute_EffectRemainingTicksChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateStateWithEffect(remainingTicks: 3);
        var changedState = state.DeepCloneForSimulation();
        changedState.ActiveEffects[new UnitInstanceId(2)][new EffectInstanceId(10)].RemainingTicks = 2;

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }

    [Fact]
    public void Compute_EffectCurrentStacksChange_ReturnsDifferentKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateStateWithEffect(currentStacks: 1);
        var changedState = state.DeepCloneForSimulation();
        changedState.ActiveEffects[new UnitInstanceId(2)][new EffectInstanceId(10)].CurrentStacks = 2;

        var originalKey = hasher.Compute(state);
        var changedKey = hasher.Compute(changedState);

        Assert.NotEqual(originalKey, changedKey);
    }
}
