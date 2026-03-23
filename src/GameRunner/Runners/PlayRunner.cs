namespace GameRunner.Runners;

using Core.Domain.Types;
using Core.Engine;
using GameRunner.Controllers;
using GameRunner.Results;

public sealed class PlayRunner : IPlayRunner
{
    public async ValueTask<PlayRunResult> RunAsync(
        EngineFacade engine,
        IReadOnlyDictionary<TeamId, IPlayerController> controllers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(controllers);

        var appliedActionCount = 0;

        while (!engine.IsGameOver())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var teamToAct = engine.GetState().Turn.TeamToAct;
            var legalActions = engine.GetLegalActions().ToList().AsReadOnly();

            if (legalActions.Count == 0)
                throw new InvalidOperationException($"No legal actions are available for team '{teamToAct}'.");

            if (!controllers.TryGetValue(teamToAct, out var controller))
                throw new InvalidOperationException($"No player controller is registered for team '{teamToAct}'.");

            var context = new PlayerTurnContext(engine, teamToAct, legalActions);
            var chosenAction = await controller.ChooseActionAsync(context, cancellationToken);

            ValidateChosenAction(chosenAction);

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

            appliedActionCount++;
        }

        return new PlayRunResult(engine.GetOutcome(), appliedActionCount);
    }

    private static void ValidateChosenAction(Core.Engine.Actions.Choice.ActionChoice action)
    {
        ArgumentNullException.ThrowIfNull(action);
    }
}
