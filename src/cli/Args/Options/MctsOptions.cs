namespace Cli.Args.Options;

using Agents.Mcts.Config;

public sealed class MctsOptions
{
    public static MctsOptions Default { get; } = new();

    public static MctsOptions AttackerDefault { get; } = new()
    {
        Profile = MctsAgentProfile.Offensive("Attacker")
    };

    public static MctsOptions DefenderDefault { get; } = new()
    {
        Profile = MctsAgentProfile.Defensive("Defender")
    };

    public int IterationBudget { get; init; } = 128;

    public int MaxDepth { get; init; } = 6;

    public int RandomSeed { get; init; } = 12345;

    public int MaxAttackerTurns { get; init; } = 12;

    public MctsRolloutPolicy RolloutPolicy { get; init; } = MctsRolloutPolicy.Heuristic;

    public MctsAgentProfile Profile { get; init; } = MctsAgentProfile.Balanced();
}
