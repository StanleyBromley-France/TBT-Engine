using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Effects;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Engine.Undo;
using Core.Game;
using Core.Map.Grid;
using Core.Map.Pathfinding;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Actions.Execution;

public class UseAbilityActionHandlerTests
{
    [Fact]
    public void Execute_SingleTarget_Applies_Effect_And_Consumes_Resources_And_Undo_Restores()
    {
        // Arrange: single-target enemy ability
        var ability = EngineTestFactory.CreateAbility(
            id: "strike",
            manaCost: 3,
            targetType: TargetType.Enemy,
            range: 1,
            radius: 0,
            effectId: "strike-effect");

        // Arrange: unit instances and state
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(1, 1), mana: 10, abilityIds: ability.Id);
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(2, 1));
        var state = EngineTestFactory.CreateState(new[] { caster, target }, teamToAct: 1, activeUnitId: caster.Id);

        // Arrange: repositories/services/handler
        var abilities = new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) });
        var session = EngineTestFactory.CreateSession(state, abilities);
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);
        var effectManager = new SpyEffectManager();
        var handler = new UseAbilityActionHandler(abilities, new StubPathfinder(), effectManager);

        // Act: cast directly on one target
        handler.Execute(state, context, new UseAbilityAction(caster.Id, ability.Id, target.Id));

        // Assert: resources spent, caster committed, and correct effect request dispatched
        Assert.Equal(7, caster.Resources.Mana);
        Assert.Equal(1, caster.Resources.ActionPoints);
        Assert.True(state.Phase.HasCommitted(caster.Id));
        Assert.Equal(1, effectManager.CallCount);
        Assert.NotNull(effectManager.LastRequest);
        Assert.Equal(ability.Effect, effectManager.LastRequest!.TemplateId);
        Assert.Equal(caster.Id, effectManager.LastRequest.SourceUnitId);
        Assert.Single(effectManager.LastRequest.TargetUnitIds);
        Assert.Equal(target.Id, effectManager.LastRequest.TargetUnitIds[0]);

        // Assert: undo restores resources and commit state
        undo.UndoAll(state);
        Assert.Equal(10, caster.Resources.Mana);
        Assert.Equal(2, caster.Resources.ActionPoints);
        Assert.False(state.Phase.HasCommitted(caster.Id));
    }

    [Theory]
    [InlineData(TargetType.Enemy, 2)]
    [InlineData(TargetType.Ally, 4)]
    [InlineData(TargetType.Self, 1)]
    public void Execute_Aoe_Filters_By_TargetType_And_LineOfSight(TargetType targetType, int expectedTargetId)
    {
        // Arrange: AoE enemy-only ability with LOS required
        var ability = EngineTestFactory.CreateAbility(
            id: "test",
            manaCost: 2,
            targetType: targetType,
            range: 5,
            requiresLos: true,
            radius: 1,
            effectId: "test-effect");

        // Arrange: units around chosen base target
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(1, 1), mana: 10, abilityIds: ability.Id);
        var selectedEnemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(2, 1));
        var blockedEnemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(2, 0));
        var allyInRadius = EngineTestFactory.CreateUnit(4, 1, new HexCoord(1, 2));

        var state = EngineTestFactory.CreateState(
            new[] { caster, selectedEnemy, blockedEnemy, allyInRadius },
            teamToAct: 1,
            activeUnitId: caster.Id);

        // Arrange: pathfinder blocks LOS only to blockedEnemy
        var abilities = new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) });
        var session = EngineTestFactory.CreateSession(state, abilities);
        var context = EngineTestFactory.CreateContext(session);
        var pathfinder = new StubPathfinder(blockedEnemy.Position);
        var effectManager = new SpyEffectManager();
        var handler = new UseAbilityActionHandler(abilities, pathfinder, effectManager);

        // Act: cast on selectedEnemy; candidates in radius are then filtered by target type + LOS
        handler.Execute(state, context, new UseAbilityAction(caster.Id, ability.Id, selectedEnemy.Id));

        // Assert: only the target type match with clear LOS remains
        Assert.NotNull(effectManager.LastRequest);
        Assert.Equal(new[] { new UnitInstanceId(expectedTargetId) }, effectManager.LastRequest!.TargetUnitIds);
    }

    [Fact]
    public void Execute_Aoe_Includes_Multiple_Matching_Targets()
    {
        // Arrange: enemy-targeted AoE where two enemies are inside radius with clear LOS
        var ability = EngineTestFactory.CreateAbility(
            id: "test",
            manaCost: 2,
            targetType: TargetType.Enemy,
            range: 5,
            requiresLos: true,
            radius: 1,
            effectId: "test-effect");

        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(1, 1), mana: 10, abilityIds: ability.Id);
        var selectedEnemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(2, 1));
        var nearbyEnemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(2, 0));
        var farEnemy = EngineTestFactory.CreateUnit(5, 2, new HexCoord(5, 5));

        var state = EngineTestFactory.CreateState(
            new[] { caster, selectedEnemy, nearbyEnemy, farEnemy },
            teamToAct: 1,
            activeUnitId: caster.Id);

        var abilities = new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) });
        var session = EngineTestFactory.CreateSession(state, abilities);
        var context = EngineTestFactory.CreateContext(session);
        var effectManager = new SpyEffectManager();
        var handler = new UseAbilityActionHandler(abilities, new StubPathfinder(), effectManager);

        // Act: cast on selectedEnemy, should include selected + nearby enemy only
        handler.Execute(state, context, new UseAbilityAction(caster.Id, ability.Id, selectedEnemy.Id));

        // Assert: both matching enemies in radius are targeted
        Assert.NotNull(effectManager.LastRequest);
        Assert.Equal(2, effectManager.LastRequest!.TargetUnitIds.Length);
        Assert.Contains(selectedEnemy.Id, effectManager.LastRequest.TargetUnitIds);
        Assert.Contains(nearbyEnemy.Id, effectManager.LastRequest.TargetUnitIds);
    }

    private sealed class SpyEffectManager : IEffectManager
    {
        public int CallCount { get; private set; }
        public EffectApplicationRequest? LastRequest { get; private set; }

        public void ApplyOrStackEffect(GameMutationContext context, IReadOnlyGameState state, EffectApplicationRequest request)
        {
            CallCount++;
            LastRequest = new EffectApplicationRequest(request.TemplateId, request.SourceUnitId, request.TargetUnitIds.ToArray());
        }

        public void TickAll(GameMutationContext context, IReadOnlyGameState state)
        {
        }
    }

    private sealed class StubPathfinder : IPathfinder
    {
        private readonly HexCoord _blockedPosition;

        public StubPathfinder(HexCoord blockedPosition)
        {
            _blockedPosition = blockedPosition;
        }

        public StubPathfinder()
            : this(new HexCoord(int.MinValue, int.MinValue))
        {
        }

        public IReadOnlyDictionary<HexCoord, int> GetReachable(IReadOnlyMap map, HexCoord start, int maxMoves) =>
            new Dictionary<HexCoord, int> { [start] = 0 };

        public bool IsMoveValid(IReadOnlyMap map, HexCoord start, HexCoord destination, int maxMoves) => true;

        public int? GetMoveCost(IReadOnlyMap map, HexCoord start, HexCoord destination) => 1;

        public bool HasLineOfSight(IReadOnlyMap map, HexCoord from, HexCoord to) => to != _blockedPosition;
    }
}
