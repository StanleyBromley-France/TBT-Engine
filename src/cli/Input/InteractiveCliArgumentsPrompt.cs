namespace Cli.Input;

using Cli.Args.Defaults;
using Cli.Args.Commands;
using Cli.Args.Models;
using Cli.Args.Options;

public sealed class InteractiveCliArgumentsPrompt
{
    public CliArguments Prompt()
    {
        var command = PromptCommand();

        return command switch
        {
            Command.Play => CliArguments.CreatePlay(CommandDefaults.CreatePlayOptions()),
            Command.Eval => CliArguments.CreateEval(PromptEvalOptions()),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'.")
        };
    }

    private static EvalOptions PromptEvalOptions()
    {
        var defaults = CommandDefaults.CreateEvalOptions();
        var repeatCount = PromptPositiveInt("Repeat count", defaults.RepeatCount);

        return new EvalOptions
        {
            ContentPath = defaults.ContentPath,
            ValidationMode = defaults.ValidationMode,
            Seed = defaults.Seed,
            RepeatCount = repeatCount,
            MaxTurns = defaults.MaxTurns,
            AttackerMcts = defaults.AttackerMcts,
            DefenderMcts = defaults.DefenderMcts,
            EvalRunResultOutput = defaults.EvalRunResultOutput,
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

    private static int PromptPositiveInt(string label, int defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            var raw = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            if (int.TryParse(raw, out var value) && value > 0)
                return value;

            Console.WriteLine($"{label} must be a positive integer.");
        }
    }
}
