namespace GameRunner.Runners.Observers;

using Core.Engine.Actions.Choice;
using GameRunner.Results;

public interface IEvalRunObserver
{
    void OnScenarioStarted(string scenarioId);

    void OnTurnStarted(string scenarioId, int attackerTurnsTaken, int teamToAct);

    void OnActionChosen(string scenarioId, int actionIndex, ActionChoice action, TimeSpan selectionDuration);

    void OnScenarioCompleted(string scenarioId, int repeatIndex, int runSeed, EvalRunResult result, TimeSpan totalDuration);
}
