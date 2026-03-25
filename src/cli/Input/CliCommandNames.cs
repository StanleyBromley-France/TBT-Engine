namespace Cli.Input;

using Cli.Args.Commands;

public static class CliCommandNames
{
    public const string Play = "play";
    public const string Eval = "eval";

    public static IReadOnlyList<CommandPromptChoice> PromptChoices { get; } =
    [
        new(Command.Play, "1", Play),
        new(Command.Eval, "2", Eval)
    ];

    public static IReadOnlyList<string> InteractiveTokens { get; } =
        PromptChoices.SelectMany(static choice => choice.AllTokens).ToArray();

    public static bool TryParseInteractive(string? raw, out Command command)
    {
        var value = raw?.Trim();
        foreach (var choice in PromptChoices)
        {
            if (string.Equals(value, choice.Number, StringComparison.Ordinal) ||
                string.Equals(value, choice.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                command = choice.Command;
                return true;
            }
        }

        command = default;
        return false;
    }

    public static bool Is(string? value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record CommandPromptChoice(
    Command Command,
    string Number,
    string DisplayName)
{
    public IReadOnlyList<string> AllTokens => [Number, DisplayName];
}
