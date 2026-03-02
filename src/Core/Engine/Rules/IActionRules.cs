namespace Core.Engine.Rules;

using Core.Game;
using Core.Domain.Types;
using Core.Engine.Actions.Choice;

public interface IActionRules
{
    IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state);
    bool IsActionLegal(IReadOnlyGameState state, ActionChoice action);
    TeamId? GetWinner(IReadOnlyGameState state);
}