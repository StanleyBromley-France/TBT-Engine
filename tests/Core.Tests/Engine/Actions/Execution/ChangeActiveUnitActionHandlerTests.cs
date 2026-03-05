using Core.Domain.Abilities;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Random;
using Core.Engine.Undo;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Actions.Execution;

public class ChangeActiveUnitActionHandlerTests
{
    [Fact]
    public void Execute_Changes_Active_Unit_And_Undo_Restores()
    {
        // Arrange: two allied units with unitA initially active
        var unitA = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var unitB = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0));
        var enemy = EngineTestFactory.CreateUnit(3, 2, new HexCoord(3, 0));
        var state = EngineTestFactory.CreateState(new[] { unitA, unitB, enemy }, teamToAct: 1, activeUnitId: unitA.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var ctx = new Core.Engine.Mutation.GameMutationContext(session, new DeterministicRng(), undo);
        var handler = new ChangeActiveUnitActionHandler();

        // Act: switch active unit to unitB
        handler.Execute(state, ctx, new ChangeActiveUnitAction(unitA.Id, unitB.Id));

        // Assert: active unit changed
        Assert.Equal(unitB.Id, state.Phase.ActiveUnitId);

        // Assert: undo restores previous active unit
        undo.UndoAll(state);

        Assert.Equal(unitA.Id, state.Phase.ActiveUnitId);
    }
}
