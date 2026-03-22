namespace Cli.Resolution.Models;

using Cli.Args.Commands;
using Cli.Args.Options;

public sealed record EvalCommand(EvalOptions Options) : ResolvedCommand(Command.Eval);
