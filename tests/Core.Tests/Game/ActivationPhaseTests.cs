using Core.Domain.Types;
using Core.Game.State;

namespace Core.Tests.Game;

public class ActivationPhaseTests
{
    [Fact]
    public void Constructor_Sets_Empty_PhaseState()
    {
        var phase = new ActivationPhase();

        Assert.Empty(phase.CommittedThisPhase);
        Assert.Null(phase.CurrentlyCommiting);
    }

    [Fact]
    public void MarkCommitted_Then_HasCommitted_Returns_True()
    {
        var phase = new ActivationPhase();
        var committed = new UnitInstanceId(2);

        phase.MarkCommitted(committed);

        Assert.True(phase.HasCommitted(committed));
    }

    [Fact]
    public void Reset_Clears_PhaseState()
    {
        var phase = new ActivationPhase();
        phase.MarkCommitted(new UnitInstanceId(1));
        phase.MarkCommitted(new UnitInstanceId(2));
        phase.SetCurrentlyCommiting(new UnitInstanceId(2));

        phase.Reset();

        Assert.Empty(phase.CommittedThisPhase);
        Assert.Null(phase.CurrentlyCommiting);
    }
}
