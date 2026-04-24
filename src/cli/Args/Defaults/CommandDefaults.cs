namespace Cli.Args.Defaults;

using Cli.Args.Models;
using Cli.Args.Options;
using Setup.Validation.Primitives;

public static class CommandDefaults
{
    private static readonly string ExampleContentPath = Path.Combine(AppContext.BaseDirectory, "content");

    private static readonly string EvalOutputPath = Path.Combine(AppContext.BaseDirectory, "eval-run-result.json");

    public static PlayOptions CreatePlayOptions()
    {
        return new PlayOptions
        {
            ContentPath = ExampleContentPath,
            GameStateId = "default",
            Seed = 12345,
            MaxTurns = 12,
            ValidationMode = ContentValidationMode.Strict,
            Mode = PlayMode.ZeroPlayer,
            AttackerMcts = MctsOptions.AttackerDefault,
            DefenderMcts = MctsOptions.DefenderDefault
        };
    }

    public static EvalOptions CreateEvalOptions()
    {
        return new EvalOptions
        {
            ContentPath = ExampleContentPath,
            GameStateId = string.Empty,
            Seed = 3867623,
            RepeatCount = 1,
            Parallelism = 12,
            MaxTurns = 40,
            Quiet = false,
            Verbose = false,
            ValidationMode = ContentValidationMode.Strict,
            AttackerMcts = MctsOptions.AttackerDefault,
            DefenderMcts = MctsOptions.DefenderDefault,
            EvalRunResultOutput = EvalOutputPath,
        };
    }
}
