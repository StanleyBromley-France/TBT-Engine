namespace Agents.Mcts.Search;

using Core.Engine.Actions.Choice;
using Core.Domain.Types;

public sealed class MctsNode
{
    private readonly List<ActionChoice> _unexpandedActions;
    private readonly List<MctsNode> _children = new();

    public MctsNode(
        TeamId teamToAct,
        IEnumerable<ActionChoice> legalActions,
        ActionChoice? actionFromParent = null)
    {
        TeamToAct = teamToAct;
        ActionFromParent = actionFromParent;
        _unexpandedActions = legalActions?.ToList() ?? throw new ArgumentNullException(nameof(legalActions));
    }

    public TeamId TeamToAct { get; }
    public ActionChoice? ActionFromParent { get; }
    public int Visits { get; private set; }
    public double TotalValue { get; private set; }
    public double AverageValue => Visits == 0 ? 0d : TotalValue / Visits;
    public IReadOnlyList<MctsNode> Children => _children;
    public int UnexpandedActionCount => _unexpandedActions.Count;
    public bool CanExpand => _unexpandedActions.Count > 0;
    public bool CanSelectChild => _unexpandedActions.Count == 0 && _children.Count > 0;

    public ActionChoice RemoveUnexpandedActionAt(int index)
    {
        if (index < 0 || index >= _unexpandedActions.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var action = _unexpandedActions[index];
        _unexpandedActions.RemoveAt(index);
        return action;
    }

    public MctsNode AddChild(ActionChoice action, TeamId teamToAct, IEnumerable<ActionChoice> legalActions)
    {
        var child = new MctsNode(teamToAct, legalActions, action);
        _children.Add(child);
        return child;
    }

    public void RecordSimulation(double value)
    {
        Visits++;
        TotalValue += value;
    }
}
