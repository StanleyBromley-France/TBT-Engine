using Core.Domain.Abilities;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Tests.Engine.TestSupport;
using Core.Undo;

namespace Core.Tests.Engine.Mutation.Mutators;

public class UnitsMutatorTests
{
    [Fact]
    public void ChangeHp_When_Unit_Dies_Removes_OccupiedHex_And_Undo_Restores()
    {
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), hp: 1);
        var ally = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { unit, ally }, teamToAct: 1, activeUnitId: ally.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);

        Assert.Contains(unit.Position, state.OccupiedHexes);
        Assert.True(unit.IsAlive);

        context.Units.ChangeHp(unit.Id, -1);

        Assert.False(unit.IsAlive);
        Assert.DoesNotContain(unit.Position, state.OccupiedHexes);

        undo.UndoAll(state);

        Assert.True(unit.IsAlive);
        Assert.Contains(unit.Position, state.OccupiedHexes);
    }

    [Fact]
    public void ChangeHp_When_Unit_Revives_Adds_OccupiedHex_And_Undo_Restores()
    {
        var deadUnit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), hp: 0);
        var aliveUnit = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0), hp: 10);
        var state = EngineTestFactory.CreateState(new[] { deadUnit, aliveUnit }, teamToAct: 1, activeUnitId: aliveUnit.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);

        Assert.False(deadUnit.IsAlive);
        Assert.DoesNotContain(deadUnit.Position, state.OccupiedHexes);

        context.Units.ChangeHp(deadUnit.Id, +2);

        Assert.True(deadUnit.IsAlive);
        Assert.Contains(deadUnit.Position, state.OccupiedHexes);

        undo.UndoAll(state);

        Assert.False(deadUnit.IsAlive);
        Assert.DoesNotContain(deadUnit.Position, state.OccupiedHexes);
    }
}
