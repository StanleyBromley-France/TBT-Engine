namespace Cli.Args.Options;

public sealed class PlayOptions : ContentOptions
{
    public required string GameStateId { get; init; }

    public int Seed { get; init; }

    public int MaxTurns { get; init; }

    public PlayMode Mode { get; init; }

    public required MctsOptions AttackerMcts { get; init; }

    public required MctsOptions DefenderMcts { get; init; }
}
