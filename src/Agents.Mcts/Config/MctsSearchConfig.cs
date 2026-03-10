namespace Agents.Mcts.Config;

public sealed class MctsSearchConfig
{
    public int IterationBudget { get; set; }
    public int MaxDepth { get; set; }
    public double ExplorationConstant { get; set; }
}
