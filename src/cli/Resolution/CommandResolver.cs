namespace Cli.Resolution;

using Cli.Args.Commands;
using Cli.Args.Models;
using Cli.Resolution.Models;

public sealed class CommandResolver : ICommandResolver
{
    public ResolvedCommand Resolve(CliArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Command switch
        {
            Command.Play => new PlayCommand(arguments.PlayOptions ?? throw MissingOptions(nameof(arguments.PlayOptions))),
            Command.Eval => new EvalCommand(arguments.EvalOptions ?? throw MissingOptions(nameof(arguments.EvalOptions))),
            _ => throw new InvalidOperationException($"Unsupported command '{arguments.Command}'.")
        };
    }

    private static InvalidOperationException MissingOptions(string propertyName)
    {
        return new InvalidOperationException($"Command options were not supplied for '{propertyName}'.");
    }
}
