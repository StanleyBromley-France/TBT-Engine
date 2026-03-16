namespace Agents.Mcts.Config;

public sealed class MctsSearchConfig
{
    public MctsSearchConfig()
    {
    }

    public MctsSearchConfig(MctsAgentProfile profile, MctsAgentProfile opponentProfile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        OpponentProfile = opponentProfile ?? throw new ArgumentNullException(nameof(opponentProfile));
    }

    public int IterationBudget { get; set; } = 256;
    public int MaxDepth { get; set; } = 12;
    public double ExplorationConstant { get; set; } = 1.41d;
    public int RandomSeed { get; set; } = 12345;
    public int MaxAttackerTurns { get; set; } = 12;
    public MctsRolloutPolicy RolloutPolicy { get; set; } = MctsRolloutPolicy.Heuristic;
    public MctsAgentProfile Profile { get; set; } = MctsAgentProfile.Balanced();
    public MctsAgentProfile OpponentProfile { get; set; } = MctsAgentProfile.Balanced("Opponent");
}
