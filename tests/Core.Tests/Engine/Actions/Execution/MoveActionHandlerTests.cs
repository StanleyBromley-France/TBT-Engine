using Core.Domain.Abilities;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Random;
using Core.Undo;
using Core.Game;
using Core.Map.Grid;
using Core.Map.Pathfinding;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Actions.Execution;

public class MoveActionHandlerTests
{
    [Fact]
    public void Execute_Moves_Unit_Spends_Resources_And_Undo_Restores()
    {
        // Arrange: units, state, and handler with fixed move cost
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var enemy = EngineTestFactory.CreateUnit(2, 2, new HexCoord(3, 0));
        var state = EngineTestFactory.CreateState(new[] { unit, enemy }, teamToAct: 1, activeUnitId: unit.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var ctx = new Core.Engine.Mutation.GameMutationContext(session, new DeterministicRng(), undo);
        var target = new HexCoord(1, 0);
        var handler = new MoveActionHandler(new StubPathfinder(moveCost: 2));

        // Act: execute move
        handler.Execute(state, ctx, new MoveAction(unit.Id, target));

        // Assert: position/resources/occupied hexes updated
        Assert.Equal(target, unit.Position);
        Assert.Equal(1, unit.Resources.MovePoints);
        Assert.Equal(1, unit.Resources.ActionPoints);
        Assert.False(state.Phase.HasCommitted(unit.Id));
        Assert.Null(state.Phase.CurrentlyCommiting);
        Assert.Contains(target, state.OccupiedHexes);
        Assert.DoesNotContain(new HexCoord(0, 0), state.OccupiedHexes);

        // Assert: undo restores all mutated state
        undo.UndoAll(state);

        Assert.Equal(new HexCoord(0, 0), unit.Position);
        Assert.Equal(3, unit.Resources.MovePoints);
        Assert.Equal(2, unit.Resources.ActionPoints);
        Assert.False(state.Phase.HasCommitted(unit.Id));
        Assert.Null(state.Phase.CurrentlyCommiting);
        Assert.Contains(new HexCoord(0, 0), state.OccupiedHexes);
        Assert.DoesNotContain(target, state.OccupiedHexes);
    }

    private sealed class StubPathfinder : IPathfinder
    {
        private readonly int _moveCost;

        public StubPathfinder(int moveCost)
        {
            _moveCost = moveCost;
        }

        public IReadOnlyDictionary<HexCoord, int> GetReachable(IReadOnlyMap map, HexCoord start, int maxMoves) =>
            new Dictionary<HexCoord, int> { [start] = 0 };

        public bool IsMoveValid(IReadOnlyMap map, HexCoord start, HexCoord destination, int maxMoves) => true;

        public int? GetMoveCost(IReadOnlyMap map, HexCoord start, HexCoord destination) => _moveCost;

        public bool HasLineOfSight(IReadOnlyMap map, HexCoord from, HexCoord to) => true;
    }
}
