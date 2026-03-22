using Cli.Args.Models;
using Cli.Input;
using Cli.Resolution;

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

    Console.WriteLine($"CLI scaffold ready for '{command.Command}' commands.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
