namespace Cli.Eval;

using Agents.Mcts.Config;
using Agents.Mcts.Evaluation;
using Agents.Mcts.Hashing;
using Agents.Mcts.Search;
using Cli.Args.Options;
using Cli.Eval.Observer;
using Cli.Eval.Results;
using Core.Domain.Types;
using Core.Engine;
using Core.Game.Bootstrap;
using Core.Game.Bootstrap.Contracts;
using GameRunner.Controllers;
using GameRunner.Runners;
using Setup.Build.Scenarios;
using Setup.ScenarioSetup;

internal sealed class EvalCommandRunner
{
    private readonly ConsoleEvalRunObserver _observer = new();

    public async Task<EvalBatchResult> RunAsync(EvalOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        IScenarioSetup setup = new ScenarioSetup();

        var source = setup.Load(options.ContentPath, options.ValidationMode);


        var scenarioResults = new List<EvalScenarioResult>();

        foreach (var gameStateId in source.GameStateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scenario = setup.Create(
                source,
                gameStateId,
                options.ValidationMode);

            EnsureScenarioIsValid(scenario, gameStateId);

            var session = GameSessionBootstrapper.Create(
                scenario.TemplateRegistry!,
                scenario.GameStateSpec!,
                options.Seed);
            var engine = EngineCompositionRoot.Create(session, options.MaxTurns);

            _observer.RegisterScenario(gameStateId, scenario.GameStateSpec!);
            var controllers = CreateControllers(options, scenario.GameStateSpec!);
            var runner = new EvalRunner();
            var result = await runner.RunAsync(gameStateId, engine, controllers, _observer, cancellationToken);
            scenarioResults.Add(new EvalScenarioResult(gameStateId, result));
        }

        var batchResult = new EvalBatchResult(scenarioResults);
        await EvalBatchResultWriter.WriteAsync(batchResult, options.EvalRunResultOutput, cancellationToken);
        return batchResult;
    }

    private static IReadOnlyDictionary<TeamId, IPlayerController> CreateControllers(
        EvalOptions options,
        IGameStateSpec gameStateSpec)
    {
        var attackerConfig = CreateSearchConfig(options.AttackerMcts, options.DefenderMcts.Profile);
        var defenderConfig = CreateSearchConfig(options.DefenderMcts, options.AttackerMcts.Profile);

        return new Dictionary<TeamId, IPlayerController>
        {
            [gameStateSpec.AttackerTeamId] = new MctsPlayerController(CreateSearch(), attackerConfig),
            [gameStateSpec.DefenderTeamId] = new MctsPlayerController(CreateSearch(), defenderConfig),
        };
    }

    private static IMctsSearch CreateSearch()
        => new MctsSearch(new MaterialStateEvaluator(), new GameStateHasher());

    private static MctsSearchConfig CreateSearchConfig(MctsOptions options, MctsAgentProfile opponentProfile)
    {
        return new MctsSearchConfig(options.Profile, opponentProfile)
        {
            IterationBudget = options.IterationBudget,
            MaxDepth = options.MaxDepth,
            RandomSeed = options.RandomSeed,
            MaxAttackerTurns = options.MaxAttackerTurns,
            RolloutPolicy = options.RolloutPolicy,
        };
    }

    private static void EnsureScenarioIsValid(ScenarioResult scenario, string gameStateId)
    {
        if (scenario.HasErrors)
        {
            var message = string.Join(
                Environment.NewLine,
                scenario.IssueView.Issues.Select(issue => $"{issue.Severity}: {issue.Path ?? gameStateId}: {issue.Message}"));
            throw new InvalidOperationException(message);
        }

        if (scenario.TemplateRegistry is null || scenario.GameStateSpec is null)
            throw new InvalidOperationException($"Scenario bootstrap did not produce a template registry and game state spec for '{gameStateId}'.");
    }
}
