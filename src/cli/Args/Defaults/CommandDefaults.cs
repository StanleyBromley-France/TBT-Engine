namespace Cli.Args.Defaults;

using Cli.Args.Models;
using Cli.Args.Options;
using Setup.Validation.Primitives;

public static class CommandDefaults
{
    public static CliArguments CreatePlayArguments()
    {
        return CliArguments.CreatePlay(new PlayOptions
        {
            ContentPath = @"content",
            GameStateId = "default",
            Seed = 12345,
            MaxTurns = 12,
            ValidationMode = ContentValidationMode.Strict,
            Mode = PlayMode.ZeroPlayer,
            AttackerMcts = MctsOptions.AttackerDefault,
            DefenderMcts = MctsOptions.DefenderDefault
        });
    }

    public static CliArguments CreateEvalArguments()
    {
        return CliArguments.CreateEval(new EvalOptions
        {
            ContentPath = @"content",
            Seed = 12345,
            MaxTurns = 12,
            ValidationMode = ContentValidationMode.Strict,
            AttackerMcts = MctsOptions.AttackerDefault,
            DefenderMcts = MctsOptions.DefenderDefault,
            EvalRunResultOutput = @"path",
        });
    }
}
