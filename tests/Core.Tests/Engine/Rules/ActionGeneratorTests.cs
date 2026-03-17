using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Rules;
using Core.Game.State.ReadOnly;
using Core.Map.Grid;
using Core.Map.Pathfinding;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Rules;

public class ActionGeneratorTests
{
    [Fact]
    public void GetLegalActions_Generates_AbilityActions_For_Valid_Enemy_Targets()
    {
        var ability = EngineTestFactory.CreateAbility("test", manaCost: 2, targetType: TargetType.Enemy, range: 5);
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10, abilityIds: ability.Id);
        var ally = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0));
        var enemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(1, -1));
        var state = EngineTestFactory.CreateState(new[] { caster, ally, enemy }, teamToAct: 1);

        var generator = new ActionGenerator(
            new StubPathfinder(),
            new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) }),
            new AllowAllValidator());

        var useActions = generator.GetLegalActions(state).OfType<UseAbilityAction>().ToList();

        Assert.Single(useActions);
        Assert.Equal(caster.Id, useActions[0].UnitId);
        Assert.Equal(ability.Id, useActions[0].AbilityId);
        Assert.Equal(enemy.Id, useActions[0].Target);
    }

    [Fact]
    public void GetLegalActions_DoesNot_Generate_AbilityActions_When_Mana_Is_Insufficient()
    {
        var ability = EngineTestFactory.CreateAbility("test", manaCost: 50, targetType: TargetType.Enemy, range: 5);
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10, abilityIds: ability.Id);
        var enemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { caster, enemy }, teamToAct: 1);

        var generator = new ActionGenerator(
            new StubPathfinder(),
            new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) }),
            new AllowAllValidator());

        var useActions = generator.GetLegalActions(state).OfType<UseAbilityAction>().ToList();

        Assert.Empty(useActions);
    }

    private sealed class StubPathfinder : IPathfinder
    {
        public IReadOnlyDictionary<HexCoord, int> GetReachable(IReadOnlyMap map, HexCoord start, int maxMoves) =>
            new Dictionary<HexCoord, int> { [start] = 0 };

        public bool IsMoveValid(IReadOnlyMap map, HexCoord start, HexCoord destination, int maxMoves) => true;

        public int? GetMoveCost(IReadOnlyMap map, HexCoord start, HexCoord destination) => 1;

        public bool HasLineOfSight(IReadOnlyMap map, HexCoord from, HexCoord to) => true;
    }

    private sealed class AllowAllValidator : IActionValidator
    {
        public bool IsActionLegal(IReadOnlyGameState state, ActionChoice action) => true;
    }
}
