namespace GameRunner.Controllers;

using Core.Engine.Actions.Choice;

public interface IPlayerController
{
    ValueTask<ActionChoice> ChooseActionAsync(IPlayerTurnContext context, CancellationToken cancellationToken = default);
}
