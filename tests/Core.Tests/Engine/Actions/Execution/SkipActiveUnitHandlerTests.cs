using Core.Domain.Abilities;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Random;
using Core.Undo;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Actions.Execution;

public class SkipActiveUnitHandlerTests
{
    [Fact]
    public void Execute_Sets_ActionPoints_To_Zero_And_Undo_Restores()
    {
        // Arrange: active unit with AP and fresh undo context
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var enemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(2, 0));
        var state = EngineTestFactory.CreateState(new[] { unit, enemy }, teamToAct: 1, activeUnitId: unit.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var ctx = new Core.Engine.Mutation.GameMutationContext(session, new DeterministicRng(), undo);
        var handler = new SkipActiveUnitHandler();

        // Act: skip active unit
        handler.Execute(state, ctx, new SkipActiveUnitAction(unit.Id));

        // Assert: AP set to zero; phase bookkeeping is handled by ActionDispatcher
        Assert.Equal(0, unit.Resources.ActionPoints);
        Assert.False(state.Phase.HasCommitted(unit.Id));
        Assert.Null(state.Phase.CurrentlyCommiting);

        // Assert: undo restores AP and commit state
        undo.UndoAll(state);

        Assert.Equal(2, unit.Resources.ActionPoints);
        Assert.False(state.Phase.HasCommitted(unit.Id));
        Assert.Null(state.Phase.CurrentlyCommiting);
    }

    [Fact]
    public void Execute_When_Already_At_Zero_ActionPoints_Does_Not_Add_Undo_Steps()
    {
        // Arrange: unit already at zero AP
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        unit.Resources.ActionPoints = 0;
        var enemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(2, 0));
        var state = EngineTestFactory.CreateState(new[] { unit, enemy }, teamToAct: 1, activeUnitId: unit.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var ctx = new Core.Engine.Mutation.GameMutationContext(session, new DeterministicRng(), undo);
        var handler = new SkipActiveUnitHandler();

        // Act: execute skip on no-op state
        handler.Execute(state, ctx, new SkipActiveUnitAction(unit.Id));

        // Assert: no undo entries and state remains unchanged
        Assert.True(undo.IsEmpty);
        Assert.Equal(0, unit.Resources.ActionPoints);
        Assert.False(state.Phase.HasCommitted(unit.Id));
        Assert.Null(state.Phase.CurrentlyCommiting);
    }
}
