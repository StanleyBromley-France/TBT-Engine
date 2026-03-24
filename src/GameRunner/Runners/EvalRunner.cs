namespace GameRunner.Runners;

using Core.Domain.Types;
using Core.Engine;
using Core.Engine.Actions.Choice;
using GameRunner.Controllers;
using GameRunner.Results;
using GameRunner.Runners.Observers;

public sealed class EvalRunner : IEvalRunner
{
    public async ValueTask<EvalRunResult> RunAsync(
        string scenarioId,
        EngineFacade engine,
        IReadOnlyDictionary<TeamId, IPlayerController> controllers,
        IEvalRunObserver observer,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(controllers);

        var actionRecords = new List<EvalActionRecord>();
        var scenarioStopwatch = System.Diagnostics.Stopwatch.StartNew();
        int? lastObservedTeamToAct = null;

        observer.OnScenarioStarted(scenarioId);

        while (!engine.IsGameOver())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var state = engine.GetState();
            var teamToAct = state.Turn.TeamToAct;
            if (lastObservedTeamToAct != teamToAct.Value)
            {
                observer.OnTurnStarted(scenarioId, state.Turn.AttackerTurnsTaken, teamToAct.Value);
                lastObservedTeamToAct = teamToAct.Value;
            }
            var legalActions = engine.GetLegalActions().ToList().AsReadOnly();

            if (legalActions.Count == 0)
                throw new InvalidOperationException($"No legal actions are available for team '{teamToAct}'.");

            if (!controllers.TryGetValue(teamToAct, out var controller))
                throw new InvalidOperationException($"No player controller is registered for team '{teamToAct}'.");

            var context = new PlayerTurnContext(engine, teamToAct, legalActions);

            // Records time taken for action
            var actionStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var chosenAction = await controller.ChooseActionAsync(context, cancellationToken);
            actionStopwatch.Stop();

            ValidateChosenAction(chosenAction);
            observer.OnActionChosen(scenarioId, actionRecords.Count + 1, chosenAction, actionStopwatch.Elapsed);
            actionRecords.Add(CreateActionRecord(state.Turn.AttackerTurnsTaken, teamToAct, chosenAction));

            try
            {
                engine.ApplyAction(chosenAction);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Controller '{controller.GetType().Name}' returned an illegal action for team '{teamToAct}': {FormatAction(chosenAction)}.",
                    ex);
            }
        }

        scenarioStopwatch.Stop();
        var result = EvalRunResult.From(engine.GetOutcome(), actionRecords);
        observer.OnScenarioCompleted(scenarioId, result, scenarioStopwatch.Elapsed);
        return result;
    }

    private static void ValidateChosenAction(ActionChoice action)
    {
        ArgumentNullException.ThrowIfNull(action);
    }

    private static EvalActionRecord CreateActionRecord(int turnIndex, TeamId actingTeam, ActionChoice action)
    {
        return action switch
        {
            UseAbilityAction useAbility => new EvalActionRecord(
                turnIndex,
                actingTeam.Value,
                nameof(UseAbilityAction),
                useAbility.UnitId.Value,
                useAbility.AbilityId.Value,
                useAbility.Target.Value,
                null,
                null),
            MoveAction move => new EvalActionRecord(
                turnIndex,
                actingTeam.Value,
                nameof(MoveAction),
                move.UnitId.Value,
                null,
                null,
                move.TargetHex.Q,
                move.TargetHex.R),
            SkipActiveUnitAction skip => new EvalActionRecord(
                turnIndex,
                actingTeam.Value,
                nameof(SkipActiveUnitAction),
                skip.UnitId.Value,
                null,
                null,
                null,
                null),
            _ => new EvalActionRecord(
                turnIndex,
                actingTeam.Value,
                action.GetType().Name,
                action.UnitId.Value,
                null,
                null,
                null,
                null)
        };
    }

    private static string FormatAction(ActionChoice action)
    {
        return action switch
        {
            MoveAction move => $"Move(unit={move.UnitId}, target={move.TargetHex})",
            UseAbilityAction useAbility => $"UseAbility(unit={useAbility.UnitId}, ability={useAbility.AbilityId}, target={useAbility.Target})",
            SkipActiveUnitAction skip => $"Skip(unit={skip.UnitId})",
            _ => $"{action.GetType().Name}(unit={action.UnitId})"
        };
    }
}
