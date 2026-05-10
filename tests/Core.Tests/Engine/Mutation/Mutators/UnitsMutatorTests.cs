using Core.Domain.Abilities;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Effects.Templates;
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
        var state = EngineTestFactory.CreateState(new[] { unit, ally }, teamToAct: 1);
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
        var state = EngineTestFactory.CreateState(new[] { deadUnit, aliveUnit }, teamToAct: 1);
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

    [Fact]
    public void ChangeHp_When_Healing_WouldExceedMaxHp_ClampsToMaxHp_AndUndoRestores()
    {
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), hp: 8);
        var ally = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0), hp: 10);
        var state = EngineTestFactory.CreateState(new[] { unit, ally }, teamToAct: 1);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);

        context.Units.ChangeHp(unit.Id, +5);

        Assert.Equal(unit.DerivedStats.MaxHP, unit.Resources.HP);

        undo.UndoAll(state);

        Assert.Equal(8, unit.Resources.HP);
    }

    [Fact]
    public void ChangeHp_When_DeadUnitSharesHexWithAliveUnit_Undo_DoesNotClear_LiveOccupancy()
    {
        var sharedHex = new HexCoord(3, 1);
        var deadUnit = EngineTestFactory.CreateUnit(1, 1, sharedHex, hp: 0);
        var aliveUnit = EngineTestFactory.CreateUnit(2, 1, sharedHex, hp: 10);
        var otherAliveUnit = EngineTestFactory.CreateUnit(3, 1, new HexCoord(1, 0), hp: 10);
        var state = EngineTestFactory.CreateState(new[] { deadUnit, aliveUnit, otherAliveUnit }, teamToAct: 1);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);

        Assert.False(deadUnit.IsAlive);
        Assert.True(aliveUnit.IsAlive);
        Assert.Contains(sharedHex, state.OccupiedHexes);

        context.Units.ChangeHp(deadUnit.Id, -1);

        Assert.False(deadUnit.IsAlive);
        Assert.Contains(sharedHex, state.OccupiedHexes);

        undo.UndoAll(state);

        Assert.False(deadUnit.IsAlive);
        Assert.True(aliveUnit.IsAlive);
        Assert.Contains(sharedHex, state.OccupiedHexes);
    }

    [Fact]
    public void ChangeHp_When_UnitDies_RemovesActiveEffects_AndUndoRestores()
    {
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), hp: 1);
        var ally = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0), hp: 10);
        var state = EngineTestFactory.CreateState(new[] { unit, ally }, teamToAct: 1);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);
        var effect = CreateEffect(unit.Id);

        state.ActiveEffects[unit.Id].Add(effect.Id, effect);

        context.Units.ChangeHp(unit.Id, -1);

        Assert.False(unit.IsAlive);
        Assert.False(state.ActiveEffects.ContainsKey(unit.Id));

        undo.UndoAll(state);

        Assert.True(unit.IsAlive);
        Assert.True(state.ActiveEffects.ContainsKey(unit.Id));
        Assert.Contains(effect.Id, state.ActiveEffects[unit.Id]);
    }

    private static EffectInstance CreateEffect(UnitInstanceId targetId)
    {
        var template = new TestEffectTemplate(
            new EffectTemplateId("test-effect"),
            totalTicks: 2,
            maxStacks: 1);

        return new EffectInstance(
            new EffectInstanceId(1),
            template,
            sourceUnitId: targetId,
            targetUnitId: targetId,
            components: Array.Empty<EffectComponentInstance>());
    }

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(
            EffectTemplateId id,
            int totalTicks,
            int maxStacks)
            : base(
                id,
                name: id.Value,
                isHarmful: false,
                totalTicks: totalTicks,
                maxStacks: maxStacks,
                components: Array.Empty<EffectComponentTemplateId>())
        {
        }
    }
}
