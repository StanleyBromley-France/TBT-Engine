namespace Cli.Resolution.Models;

using Cli.Args.Commands;
using Cli.Args.Options;

public sealed record PlayCommand(PlayOptions Options) : ResolvedCommand(Command.Play);
