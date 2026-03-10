namespace Core.Engine.Rules;

using Core.Engine.Actions.Choice;
using Core.Game.State.ReadOnly;

public interface IActionGenerator
{
    IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state);
}