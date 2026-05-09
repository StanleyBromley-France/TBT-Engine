namespace Cli.Args.Defaults;

using Cli.Args.Models;
using Cli.Args.Options;
using Setup.Validation.Primitives;

public static class CommandDefaults
{
    private static readonly string DefaultContentPath = ResolveDefaultContentPath();

    private static readonly string EvalOutputPath = Path.Combine(AppContext.BaseDirectory, "eval-run-result.json");

    public static PlayOptions CreatePlayOptions()
    {
        return new PlayOptions
        {
            ContentPath = DefaultContentPath,
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
            ContentPath = DefaultContentPath,
            GameStateId = string.Empty,
            Seed = 3867623,
            RepeatCount = 1,
            Parallelism = 12,
            MaxTurns = 14,
            Quiet = false,
            Verbose = false,
            ValidationMode = ContentValidationMode.Strict,
            AttackerMcts = MctsOptions.AttackerDefault,
            DefenderMcts = MctsOptions.DefenderDefault,
            EvalRunResultOutput = EvalOutputPath,
        };
    }

    private static string ResolveDefaultContentPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "content");
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "content");
    }
}
