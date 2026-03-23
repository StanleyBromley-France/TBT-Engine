namespace GameRunner.Runners;

using Core.Domain.Types;
using Core.Engine;
using Core.Engine.Actions.Choice;
using GameRunner.Controllers;
using GameRunner.Results;

public sealed class EvalRunner : IEvalRunner
{
    public async ValueTask<EvalRunResult> RunAsync(
        EngineFacade engine,
        IReadOnlyDictionary<TeamId, IPlayerController> controllers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(controllers);

        var actionRecords = new List<EvalActionRecord>();

        while (!engine.IsGameOver())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var state = engine.GetState();
            var teamToAct = state.Turn.TeamToAct;
            var legalActions = engine.GetLegalActions().ToList().AsReadOnly();

            if (legalActions.Count == 0)
                throw new InvalidOperationException($"No legal actions are available for team '{teamToAct}'.");

            if (!controllers.TryGetValue(teamToAct, out var controller))
                throw new InvalidOperationException($"No player controller is registered for team '{teamToAct}'.");

            var context = new PlayerTurnContext(engine, teamToAct, legalActions);
            var chosenAction = await controller.ChooseActionAsync(context, cancellationToken);

            ValidateChosenAction(chosenAction);
            actionRecords.Add(CreateActionRecord(state.Turn.AttackerTurnsTaken, teamToAct, chosenAction));

            try
            {
                engine.ApplyAction(chosenAction);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Controller '{controller.GetType().Name}' returned an illegal action for team '{teamToAct}'.",
                    ex);
            }
        }

        return EvalRunResult.From(engine.GetOutcome(), actionRecords);
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
}
