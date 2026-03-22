namespace Cli.Resolution;

using Cli.Args.Models;
using Cli.Resolution.Models;

public interface ICommandResolver
{
    ResolvedCommand Resolve(CliArguments arguments);
}
