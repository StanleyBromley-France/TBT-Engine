using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Rules;
using Core.Game;
using Core.Map.Grid;
using Core.Map.Pathfinding;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Rules;

public class ActionValidatorTests
{
    [Fact]
    public void UseAbility_IsIllegal_When_Target_Is_Out_Of_Range()
    {
        var ability = EngineTestFactory.CreateAbility("shot", manaCost: 1, targetType: TargetType.Enemy, range: 1);
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10, abilityIds: ability.Id);
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(3, 0));
        var state = EngineTestFactory.CreateState(new[] { caster, target }, teamToAct: 1, activeUnitId: caster.Id);

        var validator = new ActionValidator(
            new StubPathfinder { HasLineOfSightResult = true },
            new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) }));

        var legal = validator.IsActionLegal(state, new UseAbilityAction(caster.Id, ability.Id, target.Id));

        Assert.False(legal);
    }

    [Fact]
    public void UseAbility_IsLegal_When_Target_Is_In_Range_And_Valid()
    {
        var ability = EngineTestFactory.CreateAbility("stab", manaCost: 1, targetType: TargetType.Enemy, range: 2);
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10, abilityIds: ability.Id);
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { caster, target }, teamToAct: 1, activeUnitId: caster.Id);

        var validator = new ActionValidator(
            new StubPathfinder { HasLineOfSightResult = true },
            new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) }));

        var legal = validator.IsActionLegal(state, new UseAbilityAction(caster.Id, ability.Id, target.Id));

        Assert.True(legal);
    }

    [Fact]
    public void UseAbility_IsIllegal_When_LineOfSight_Required_But_Blocked()
    {
        var ability = EngineTestFactory.CreateAbility("beam", manaCost: 1, targetType: TargetType.Enemy, range: 3, requiresLos: true);
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10, abilityIds: ability.Id);
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { caster, target }, teamToAct: 1, activeUnitId: caster.Id);

        var validator = new ActionValidator(
            new StubPathfinder { HasLineOfSightResult = false },
            new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) }));

        var legal = validator.IsActionLegal(state, new UseAbilityAction(caster.Id, ability.Id, target.Id));

        Assert.False(legal);
    }

    [Fact]
    public void UseAbility_IsIllegal_When_Target_Type_Does_Not_Match()
    {
        var ability = EngineTestFactory.CreateAbility("enemy-only", manaCost: 1, targetType: TargetType.Enemy, range: 3);
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10, abilityIds: ability.Id);
        var ally = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { caster, ally }, teamToAct: 1, activeUnitId: caster.Id);

        var validator = new ActionValidator(
            new StubPathfinder { HasLineOfSightResult = true },
            new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) }));

        var legal = validator.IsActionLegal(state, new UseAbilityAction(caster.Id, ability.Id, ally.Id));

        Assert.False(legal);
    }

    [Fact]
    public void Move_IsIllegal_When_ActiveUnit_Has_No_ActionPoints()
    {
        var mover = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        mover.Resources.ActionPoints = 0;
        var enemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(3, 0));
        var state = EngineTestFactory.CreateState(new[] { mover, enemy }, teamToAct: 1, activeUnitId: mover.Id);

        var validator = new ActionValidator(
            new StubPathfinder { HasLineOfSightResult = true },
            new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var legal = validator.IsActionLegal(state, new MoveAction(mover.Id, new HexCoord(1, 0)));

        Assert.False(legal);
    }

    [Fact]
    public void Skip_IsIllegal_When_ActiveUnit_Has_No_ActionPoints()
    {
        var active = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        active.Resources.ActionPoints = 0;
        var enemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(3, 0));
        var state = EngineTestFactory.CreateState(new[] { active, enemy }, teamToAct: 1, activeUnitId: active.Id);

        var validator = new ActionValidator(
            new StubPathfinder { HasLineOfSightResult = true },
            new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        var legal = validator.IsActionLegal(state, new SkipActiveUnitAction(active.Id));

        Assert.False(legal);
    }

    [Fact]
    public void UseAbility_IsIllegal_When_ActiveUnit_Has_No_ActionPoints()
    {
        var ability = EngineTestFactory.CreateAbility("blast", manaCost: 1, targetType: TargetType.Enemy, range: 3);
        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), mana: 10, abilityIds: ability.Id);
        caster.Resources.ActionPoints = 0;
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { caster, target }, teamToAct: 1, activeUnitId: caster.Id);

        var validator = new ActionValidator(
            new StubPathfinder { HasLineOfSightResult = true },
            new AbilityRepository(new[] { new KeyValuePair<AbilityId, Ability>(ability.Id, ability) }));

        var legal = validator.IsActionLegal(state, new UseAbilityAction(caster.Id, ability.Id, target.Id));

        Assert.False(legal);
    }

    private sealed class StubPathfinder : IPathfinder
    {
        public bool HasLineOfSightResult { get; init; } = true;

        public IReadOnlyDictionary<HexCoord, int> GetReachable(IReadOnlyMap map, HexCoord start, int maxMoves) =>
            new Dictionary<HexCoord, int> { [start] = 0 };

        public bool IsMoveValid(IReadOnlyMap map, HexCoord start, HexCoord destination, int maxMoves) => true;

        public int? GetMoveCost(IReadOnlyMap map, HexCoord start, HexCoord destination) => 1;

        public bool HasLineOfSight(IReadOnlyMap map, HexCoord from, HexCoord to) => HasLineOfSightResult;
    }
}
