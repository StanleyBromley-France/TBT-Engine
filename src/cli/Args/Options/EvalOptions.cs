namespace Cli.Args.Options;

public sealed class EvalOptions : ContentOptions
{
    public int Seed { get; init; }

    public int RepeatCount { get; init; }

    public int MaxTurns { get; init; }

    public required MctsOptions AttackerMcts { get; init; }

    public required MctsOptions DefenderMcts { get; init; }

    public required string EvalRunResultOutput { get; init; }
}
