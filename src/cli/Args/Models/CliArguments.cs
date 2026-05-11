namespace Cli.Args.Models;

using Cli.Args.Commands;
using Cli.Args.Options;

public sealed class CliArguments
{
    public Command Command { get; init; } = Command.Eval;

    public EvalOptions? EvalOptions { get; init; }

    public static CliArguments CreateEval(EvalOptions options)
    {
        return new CliArguments
        {
            Command = Command.Eval,
            EvalOptions = options
        };
    }
}
