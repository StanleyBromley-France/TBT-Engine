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
        var gameStateId = PromptOptionalString("Scenario id (blank for all)", defaults.GameStateId);
        var repeatCount = PromptPositiveInt("Repeat count", defaults.RepeatCount);
        var verbosity = PromptEvalLogVerbosity();

        return new EvalOptions
        {
            ContentPath = defaults.ContentPath,
            ValidationMode = defaults.ValidationMode,
            GameStateId = gameStateId,
            Seed = defaults.Seed,
            RepeatCount = repeatCount,
            Parallelism = defaults.Parallelism,
            MaxTurns = defaults.MaxTurns,
            Quiet = verbosity == EvalPromptVerbosity.Quiet,
            Verbose = verbosity == EvalPromptVerbosity.Verbose,
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

    private static string PromptOptionalString(string label, string defaultValue)
    {
        Console.Write($"{label}: ");
        var raw = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return raw;
    }

    private static EvalPromptVerbosity PromptEvalLogVerbosity()
    {
        Console.WriteLine("Eval output:");
        Console.WriteLine("  1. Summary");
        Console.WriteLine("  2. Quiet");
        Console.WriteLine("  3. Verbose");

        while (true)
        {
            Console.Write("Output mode [1]: ");
            var raw = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "1", StringComparison.Ordinal))
                return EvalPromptVerbosity.Summary;

            if (string.Equals(raw, "2", StringComparison.Ordinal))
                return EvalPromptVerbosity.Quiet;

            if (string.Equals(raw, "3", StringComparison.Ordinal))
                return EvalPromptVerbosity.Verbose;

            Console.WriteLine("Enter 1, 2, or 3.");
        }
    }

    private enum EvalPromptVerbosity
    {
        Summary,
        Quiet,
        Verbose,
    }
}
