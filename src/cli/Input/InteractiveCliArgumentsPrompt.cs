namespace Cli.Input;

using Cli.Args.Defaults;
using Cli.Args.Commands;
using Cli.Args.Models;

public sealed class InteractiveCliArgumentsPrompt
{
    public CliArguments Prompt()
    {
        var command = PromptCommand();

        return command switch
        {
            Command.Play => CliArguments.CreatePlay(CommandDefaults.CreatePlayOptions()),
            Command.Eval => CliArguments.CreateEval(CommandDefaults.CreateEvalOptions()),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'.")
        };
    }

    private static Command PromptCommand()
    {
        Console.WriteLine("Select command:");
        foreach (var choice in CliCommandNames.PromptChoices)
        {
            Console.WriteLine($"  {choice.Number}. {choice.DisplayName}");
        }

        while (true)
        {
            Console.Write("Command: ");
            var raw = Console.ReadLine()?.Trim();

            if (CliCommandNames.TryParseInteractive(raw, out var command))
            {
                return command;
            }

            Console.WriteLine($"Enter one of: {string.Join(", ", CliCommandNames.InteractiveTokens)}.");
        }
    }
}
