using Core.Domain.Abilities;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Undo.Steps;
using Core.Tests.Engine.TestSupport;
using Core.Undo;
using Core.Game.State;

namespace Core.Tests.Undo;

public class UndoHistoryTests
{
    [Fact]
    public void Commit_Sets_CanUndo_And_Increases_RecordCount()
    {
        var history = new UndoHistory();

        history.Commit(new UndoRecord());

        Assert.True(history.CanUndo);
        Assert.Single(history.Records);
    }

    [Fact]
    public void Mark_Captures_Current_RecordCount()
    {
        var history = new UndoHistory();
        history.Commit(new UndoRecord());
        history.Commit(new UndoRecord());

        var marker = history.Mark();

        Assert.Equal(2, marker.RecordCount);
    }

    [Fact]
    public void UndoLast_Removes_Last_Record_And_Undoes_Its_Steps()
    {
        var history = new UndoHistory();
        var state = CreateState();

        var execution = new List<string>();
        var first = new UndoRecord();
        first.AddStep(new TrackingStep("first", execution));
        var second = new UndoRecord();
        second.AddStep(new TrackingStep("second", execution));

        history.Commit(first);
        history.Commit(second);

        history.UndoLast(state);

        Assert.Single(execution);
        Assert.Equal("second", execution[0]);
        Assert.Single(history.Records);
    }

    [Fact]
    public void UndoTo_Rewinds_To_Marker_Boundary()
    {
        var history = new UndoHistory();
        var state = CreateState();
        var execution = new List<string>();

        var record1 = new UndoRecord();
        record1.AddStep(new TrackingStep("1", execution));
        history.Commit(record1);

        var marker = history.Mark();

        var record2 = new UndoRecord();
        record2.AddStep(new TrackingStep("2", execution));
        history.Commit(record2);

        var record3 = new UndoRecord();
        record3.AddStep(new TrackingStep("3", execution));
        history.Commit(record3);

        history.UndoTo(state, marker);

        Assert.Equal(new[] { "3", "2" }, execution);
        Assert.Single(history.Records);
    }

    [Fact]
    public void Clear_Removes_All_Records_And_Resets_CanUndo()
    {
        var history = new UndoHistory();
        history.Commit(new UndoRecord());
        history.Commit(new UndoRecord());

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.Empty(history.Records);
    }

    [Fact]
    public void UndoRecord_UndoAll_Replays_Steps_In_Reverse_Order()
    {
        var record = new UndoRecord();
        var state = CreateState();
        var execution = new List<string>();
        record.AddStep(new TrackingStep("one", execution));
        record.AddStep(new TrackingStep("two", execution));
        record.AddStep(new TrackingStep("three", execution));

        record.UndoAll(state);

        Assert.Equal(new[] { "three", "two", "one" }, execution);
    }

    private static GameState CreateState()
    {
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        return EngineTestFactory.CreateState(new[] { unit }, teamToAct: 1, activeUnitId: unit.Id);
    }

    private sealed class TrackingStep : IUndoStep
    {
        private readonly string _name;
        private readonly List<string> _execution;

        public TrackingStep(string name, List<string> execution)
        {
            _name = name;
            _execution = execution;
        }

        public void Undo(GameState state)
        {
            _execution.Add(_name);
        }
    }
}
