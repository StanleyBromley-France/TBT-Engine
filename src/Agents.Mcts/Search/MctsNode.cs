namespace Agents.Mcts.Search;

using Core.Engine.Actions.Choice;

public sealed class MctsNode
{
    public MctsNode(ActionChoice? actionFromParent = null)
    {
        ActionFromParent = actionFromParent;
    }

    public ActionChoice? ActionFromParent { get; }
}
