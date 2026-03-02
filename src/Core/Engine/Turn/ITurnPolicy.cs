namespace Core.Engine.Turn;

using Core.Game;
using Core.Engine.Actions.Choice;

public interface ITurnPolicy
{
    ActionChoice ChooseAction(IReadOnlyGameState state, IReadOnlyList<ActionChoice> legalActions);
}