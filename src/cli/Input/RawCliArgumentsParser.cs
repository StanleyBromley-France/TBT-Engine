namespace Cli.Input;

using Cli.Args.Defaults;
using Cli.Args.Models;
using Cli.Input.Parsing;

public sealed class RawCliArgumentsParser
{
    public CliArguments Parse(string arg)
    {

        if (CliCommandNames.Is(arg, CliCommandNames.Play))
        {
            return CommandDefaults.CreatePlayArguments();
        }

        if (CliCommandNames.Is(arg, CliCommandNames.Eval))
        {
            return CommandDefaults.CreateEvalArguments();
        }

        throw new InvalidOperationException($"Unknown command '{arg}'.");
    }
}
