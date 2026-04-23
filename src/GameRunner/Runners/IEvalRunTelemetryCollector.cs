namespace GameRunner.Runners;

using Core.Engine.Actions.Choice;
using Core.Game.State.ReadOnly;

public interface IEvalRunTelemetryCollector
{
    void OnTeamTurnStarted(IReadOnlyGameState state);

    void OnActionChosen(IReadOnlyGameState state, ActionChoice action);
}
