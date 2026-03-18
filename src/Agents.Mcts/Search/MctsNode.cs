namespace Agents.Mcts.Search;

using Agents.Mcts.Hashing;
using Core.Engine.Actions.Choice;
using Core.Domain.Types;

public sealed class MctsNode
{
    private readonly List<ActionChoice> _unexpandedActions;
    private readonly List<MctsEdge> _children = new();

    public MctsNode(
        GameStateKey stateKey,
        TeamId teamToAct,
        IEnumerable<ActionChoice> legalActions)
    {
        StateKey = stateKey;
        TeamToAct = teamToAct;
        _unexpandedActions = legalActions.ToList();
    }

    public GameStateKey StateKey { get; }
    public TeamId TeamToAct { get; }
    public int Visits { get; private set; }
    public double TotalValue { get; private set; }
    public double AverageValue => Visits == 0 ? 0d : TotalValue / Visits;
    public IReadOnlyList<MctsEdge> OutgoingEdges => _children;
    public int UnexpandedActionCount => _unexpandedActions.Count;
    public bool CanExpand => _unexpandedActions.Count > 0;
    public bool CanSelectOutgoingEdge => _unexpandedActions.Count == 0 && _children.Count > 0;

    public ActionChoice RemoveUnexpandedActionAt(int index)
    {
        if (index < 0 || index >= _unexpandedActions.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var action = _unexpandedActions[index];
        _unexpandedActions.RemoveAt(index);
        return action;
    }

    public MctsEdge AddOutgoingEdge(ActionChoice action, MctsNode child)
    {
        var edge = new MctsEdge(action, child);
        _children.Add(edge);
        return edge;
    }

    public void RecordSimulation(double value)
    {
        Visits++;
        TotalValue += value;
    }
}
