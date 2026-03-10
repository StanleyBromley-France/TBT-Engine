using Core.Domain.Types;
using Core.Game.State;

namespace Core.Tests.Game;

public class ActivationPhaseTests
{
    [Fact]
    public void Constructor_Sets_ActiveUnit_And_Empty_CommitSet()
    {
        var activeUnit = new UnitInstanceId(5);

        var phase = new ActivationPhase(activeUnit);

        Assert.Equal(activeUnit, phase.ActiveUnitId);
        Assert.Empty(phase.CommittedThisPhase);
    }

    [Fact]
    public void MarkCommitted_Then_HasCommitted_Returns_True()
    {
        var phase = new ActivationPhase(new UnitInstanceId(1));
        var committed = new UnitInstanceId(2);

        phase.MarkCommitted(committed);

        Assert.True(phase.HasCommitted(committed));
    }

    [Fact]
    public void Reset_Clears_Committed_And_Sets_New_ActiveUnit()
    {
        var phase = new ActivationPhase(new UnitInstanceId(1));
        phase.MarkCommitted(new UnitInstanceId(1));
        phase.MarkCommitted(new UnitInstanceId(2));

        phase.Reset(new UnitInstanceId(3));

        Assert.Equal(new UnitInstanceId(3), phase.ActiveUnitId);
        Assert.Empty(phase.CommittedThisPhase);
    }
}
