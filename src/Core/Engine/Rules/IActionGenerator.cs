namespace Core.Engine.Rules;

using Core.Engine.Actions.Choice;
using Core.Game;

public interface IActionGenerator
{
    IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state);
}