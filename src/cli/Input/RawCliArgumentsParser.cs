namespace Cli.Input;

using Agents.Mcts.Config;
using Cli.Args.Defaults;
using Cli.Args.Models;
using Cli.Args.Options;

public sealed class RawCliArgumentsParser
{
    private static readonly IReadOnlyDictionary<string, string> OptionAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["content-path"] = "content",
            ["validation-mode"] = "validation",
            ["game-state-id"] = "game-state",
            ["output-path"] = "output",
            ["attacker-iteration-budget"] = "attacker-iterations",
            ["defender-iteration-budget"] = "defender-iterations",
        };

    public CliArguments Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
            throw new InvalidOperationException("A command is required. Use 'play' or 'eval'.");

        var commandToken = args[0];
        var optionMap = ParseOptions(args.Skip(1).ToArray());

        if (CliCommandNames.Is(commandToken, CliCommandNames.Play))
        {
            return ParsePlay(optionMap);
        }

        if (CliCommandNames.Is(commandToken, CliCommandNames.Eval))
        {
            return ParseEval(optionMap);
        }

        throw new InvalidOperationException($"Unknown command '{commandToken}'.");
    }

    private static CliArguments ParsePlay(IReadOnlyDictionary<string, string> options)
    {
        var defaults = CommandDefaults.CreatePlayArguments().PlayOptions
                    ?? throw new InvalidOperationException("Default play options are unavailable.");

        return CliArguments.CreatePlay(new PlayOptions
        {
            ContentPath = GetString(options, "content", defaults.ContentPath),
            ValidationMode = GetEnum(options, "validation", defaults.ValidationMode),
            GameStateId = GetString(options, "game-state", defaults.GameStateId),
            Seed = GetInt(options, "seed", defaults.Seed),
            MaxTurns = GetInt(options, "max-turns", defaults.MaxTurns),
            Mode = GetEnum(options, "mode", defaults.Mode),
            AttackerMcts = BuildMctsOptions(options, "attacker", defaults.AttackerMcts),
            DefenderMcts = BuildMctsOptions(options, "defender", defaults.DefenderMcts),
        });
    }

    private static CliArguments ParseEval(IReadOnlyDictionary<string, string> options)
    {
        var defaults = CommandDefaults.CreateEvalArguments().EvalOptions
                    ?? throw new InvalidOperationException("Default eval options are unavailable.");

        return CliArguments.CreateEval(new EvalOptions
        {
            ContentPath = GetString(options, "content", defaults.ContentPath),
            ValidationMode = GetEnum(options, "validation", defaults.ValidationMode),
            Seed = GetInt(options, "seed", defaults.Seed),
            MaxTurns = GetInt(options, "max-turns", defaults.MaxTurns),
            EvalRunResultOutput = GetString(options, "output", defaults.EvalRunResultOutput),
            AttackerMcts = BuildMctsOptions(options, "attacker", defaults.AttackerMcts),
            DefenderMcts = BuildMctsOptions(options, "defender", defaults.DefenderMcts),
        });
    }

    private static MctsOptions BuildMctsOptions(
        IReadOnlyDictionary<string, string> options,
        string side,
        MctsOptions defaults)
    {
        return new MctsOptions
        {
            IterationBudget = GetInt(options, $"{side}-iterations", defaults.IterationBudget),
            MaxDepth = GetInt(options, $"{side}-max-depth", defaults.MaxDepth),
            RandomSeed = GetInt(options, $"{side}-random-seed", defaults.RandomSeed),
            MaxAttackerTurns = GetInt(options, $"{side}-max-attacker-turns", defaults.MaxAttackerTurns),
            RolloutPolicy = GetEnum(options, $"{side}-rollout-policy", defaults.RolloutPolicy),
            Profile = GetProfile(options, $"{side}-profile", defaults.Profile, side),
        };
    }

    private static MctsAgentProfile GetProfile(
        IReadOnlyDictionary<string, string> options,
        string key,
        MctsAgentProfile fallback,
        string side)
    {
        if (!options.TryGetValue(key, out var rawValue))
            return fallback;

        return rawValue.Trim().ToLowerInvariant() switch
        {
            "balanced" => MctsAgentProfile.Balanced(ToDisplayName(side, "Balanced")),
            "offensive" => MctsAgentProfile.Offensive(ToDisplayName(side, "Offensive")),
            "defensive" => MctsAgentProfile.Defensive(ToDisplayName(side, "Defensive")),
            _ => throw new InvalidOperationException(
                $"Unsupported value '{rawValue}' for '--{key}'. Expected one of: balanced, offensive, defensive.")
        };
    }

    private static string ToDisplayName(string side, string profileName)
    {
        var prefix = string.Equals(side, "attacker", StringComparison.OrdinalIgnoreCase)
            ? "Attacker"
            : string.Equals(side, "defender", StringComparison.OrdinalIgnoreCase)
                ? "Defender"
                : side;

        return $"{prefix} {profileName}";
    }

    private static string GetString(
        IReadOnlyDictionary<string, string> options,
        string key,
        string fallback)
    {
        return options.TryGetValue(key, out var value)
            ? value
            : fallback;
    }

    private static int GetInt(
        IReadOnlyDictionary<string, string> options,
        string key,
        int fallback)
    {
        if (!options.TryGetValue(key, out var rawValue))
            return fallback;

        if (int.TryParse(rawValue, out var value))
            return value;

        throw new InvalidOperationException($"Value '{rawValue}' for '--{key}' is not a valid integer.");
    }

    private static TEnum GetEnum<TEnum>(
        IReadOnlyDictionary<string, string> options,
        string key,
        TEnum fallback)
        where TEnum : struct, Enum
    {
        if (!options.TryGetValue(key, out var rawValue))
            return fallback;

        if (Enum.TryParse<TEnum>(rawValue, ignoreCase: true, out var value))
            return value;

        throw new InvalidOperationException(
            $"Unsupported value '{rawValue}' for '--{key}'. Expected one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
    }

    private static IReadOnlyDictionary<string, string> ParseOptions(string[] tokens)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected argument '{token}'. Options must use '--name value'.");

            var optionName = token[2..].Trim();
            if (string.IsNullOrWhiteSpace(optionName))
                throw new InvalidOperationException("Option name must not be empty.");

            optionName = NormalizeOptionName(optionName);

            if (index + 1 >= tokens.Length)
                throw new InvalidOperationException($"Missing value for option '--{optionName}'.");

            var optionValue = tokens[++index];
            if (optionValue.StartsWith("--", StringComparison.Ordinal))
                throw new InvalidOperationException($"Missing value for option '--{optionName}'.");

            if (!options.TryAdd(optionName, optionValue))
                throw new InvalidOperationException($"Option '--{optionName}' was supplied more than once.");
        }

        return options;
    }

    private static string NormalizeOptionName(string optionName)
    {
        return OptionAliases.TryGetValue(optionName, out var normalized)
            ? normalized
            : optionName;
    }
}
