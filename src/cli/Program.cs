using Cli.Args.Models;
using Cli.Input;
using Cli.Eval;
using Cli.Resolution;
using Cli.Resolution.Models;

try
{
    CliArguments arguments;

    if (args.Length > 0)
    {
        var parser = new RawCliArgumentsParser();
        arguments = parser.Parse(args[0]);
    }
    else
    {
        var prompt = new InteractiveCliArgumentsPrompt();
        arguments = prompt.Prompt();
    }

    var resolver = new CommandResolver();
    var command = resolver.Resolve(arguments);

    switch (command)
    {
        case EvalCommand evalCommand:
        {
            var runner = new EvalCommandRunner();
            var result = await runner.RunAsync(evalCommand.Options);
            Console.WriteLine($"Eval complete. Scenarios={result.Scenarios.Count}, Output={Path.GetFullPath(evalCommand.Options.EvalRunResultOutput)}");
            break;
        }
        default:
            Console.WriteLine($"CLI scaffold ready for '{command.Command}' commands.");
            break;
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
