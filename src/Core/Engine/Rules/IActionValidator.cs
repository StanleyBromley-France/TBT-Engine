namespace Core.Engine.Rules;

using Core.Engine.Actions.Choice;
using Core.Game.State.ReadOnly;

public interface IActionValidator
{
    bool IsActionLegal(IReadOnlyGameState state, ActionChoice action);
}