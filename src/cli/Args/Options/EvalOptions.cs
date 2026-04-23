namespace Cli.Args.Options;

public sealed class EvalOptions : ContentOptions
{
    public string GameStateId { get; init; } = string.Empty;

    public int Seed { get; init; }

    public int RepeatCount { get; init; }

    public int Parallelism { get; init; }

    public int MaxTurns { get; init; }

    public required MctsOptions AttackerMcts { get; init; }

    public required MctsOptions DefenderMcts { get; init; }

    public required string EvalRunResultOutput { get; init; }
}
