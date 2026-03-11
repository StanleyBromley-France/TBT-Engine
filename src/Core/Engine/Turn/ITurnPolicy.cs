namespace Core.Engine.Turn;

using Core.Engine.Actions.Choice;
using Core.Game.State.ReadOnly;

public interface ITurnPolicy
{
    ActionChoice ChooseAction(TurnPolicyContext context, IReadOnlyGameState state, IReadOnlyList<ActionChoice> legalActions);
}
