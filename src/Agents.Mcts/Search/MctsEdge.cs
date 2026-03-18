namespace Agents.Mcts.Search;

using Core.Engine.Actions.Choice;

public sealed class MctsEdge
{
    public MctsEdge(ActionChoice action, MctsNode nextStateNode)
    {
        Action = action;
        NextStateNode = nextStateNode;
    }

    public ActionChoice Action { get; }
    public MctsNode NextStateNode { get; }
    public int Visits { get; private set; }
    public double TotalValue { get; private set; }
    public double AverageValue => Visits == 0 ? 0d : TotalValue / Visits;

    public void RecordSimulation(double value)
    {
        Visits++;
        TotalValue += value;
    }
}
