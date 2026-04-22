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
using Core.Map.Search;
using Core.Map.Terrain;
using Core.Random;
using GameRunner.Controllers;
using GameRunner.Runners;
using GameRunner.Results;
using Setup.Build.Scenarios;
using Setup.ScenarioSetup;

internal sealed class EvalCommandRunner
{
    private const int AttackerSearchSeedSalt = 3;
    private const int DefenderSearchSeedSalt = 4;

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
            var scenarioStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var scenario = setup.Create(
                source,
                gameStateId,
                options.ValidationMode);

            EnsureScenarioIsValid(scenario, gameStateId);

            var session = GameSessionBootstrapper.Create(
                scenario.TemplateRegistry!,
                scenario.GameStateSpec!,
                options.Seed);
            var telemetry = new UnitPerformanceTelemetryCollector();
            var engine = EngineCompositionRoot.Create(session, options.MaxTurns, telemetry);

            _observer.RegisterScenario(gameStateId, scenario.GameStateSpec!);
            var controllers = CreateControllers(options, scenario.GameStateSpec!, options.Seed);
            var runner = new EvalRunner();
            var result = await runner.RunAsync(gameStateId, engine, controllers, _observer, cancellationToken);
            var units = BuildUnitResults(engine, scenario.GameStateSpec!, telemetry);
            var teams = BuildTeamResults(units, result.Actions);
            var match = BuildMatchResult(
                engine,
                scenario.GameStateSpec!,
                result.Actions,
                options.Seed,
                options.MaxTurns,
                gameStateId);
            result = result with { Match = match, Teams = teams, Units = units };
            scenarioStopwatch.Stop();
            _observer.OnScenarioCompleted(gameStateId, result, scenarioStopwatch.Elapsed);
            scenarioResults.Add(new EvalScenarioResult(gameStateId, result));
        }

        var batchResult = new EvalBatchResult(scenarioResults);
        await EvalBatchResultWriter.WriteAsync(batchResult, options.EvalRunResultOutput, cancellationToken);
        return batchResult;
    }

    private static IReadOnlyDictionary<TeamId, IPlayerController> CreateControllers(
        EvalOptions options,
        IGameStateSpec gameStateSpec,
        int sharedSeed)
    {
        var attackerConfig = CreateSearchConfig(
            options.AttackerMcts,
            options.DefenderMcts.Profile,
            DeriveSearchSeed(sharedSeed, AttackerSearchSeedSalt, options.AttackerMcts.RandomSeed));
        var defenderConfig = CreateSearchConfig(
            options.DefenderMcts,
            options.AttackerMcts.Profile,
            DeriveSearchSeed(sharedSeed, DefenderSearchSeedSalt, options.DefenderMcts.RandomSeed));

        return new Dictionary<TeamId, IPlayerController>
        {
            [gameStateSpec.AttackerTeamId] = new MctsPlayerController(CreateSearch(), attackerConfig),
            [gameStateSpec.DefenderTeamId] = new MctsPlayerController(CreateSearch(), defenderConfig),
        };
    }

    private static IMctsSearch CreateSearch()
        => new MctsSearch(new MaterialStateEvaluator(), new GameStateHasher());

    private static MctsSearchConfig CreateSearchConfig(
        MctsOptions options,
        MctsAgentProfile opponentProfile,
        int randomSeed)
    {
        return new MctsSearchConfig(options.Profile, opponentProfile)
        {
            IterationBudget = options.IterationBudget,
            MaxDepth = options.MaxDepth,
            RandomSeed = randomSeed,
            MaxAttackerTurns = options.MaxAttackerTurns,
            RolloutPolicy = options.RolloutPolicy,
        };
    }

    private static int DeriveSearchSeed(int sharedSeed, int domainSalt, int configuredSeed)
    {
        var domainSeed = SeedDeriver.Derive(sharedSeed, domainSalt);
        return SeedDeriver.Derive(domainSeed, configuredSeed);
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

    private static IReadOnlyList<EvalUnitResult> BuildUnitResults(
        EngineFacade engine,
        IGameStateSpec gameStateSpec,
        UnitPerformanceTelemetryCollector telemetry)
    {
        var state = engine.GetState();

        return state.UnitInstances.Values
            .OrderBy(unit => unit.Id.Value)
            .Select(unit =>
            {
                var totals = telemetry.GetTotals(unit.Id.Value);
                var side = unit.Team == gameStateSpec.AttackerTeamId
                    ? "Attacker"
                    : unit.Team == gameStateSpec.DefenderTeamId
                        ? "Defender"
                        : "Unknown";

                return new EvalUnitResult(
                    UnitInstanceId: unit.Id.Value,
                    UnitTemplateId: unit.Template.Id.Value,
                    TeamId: unit.Team.Value,
                    Side: side,
                    Roles: new EvalUnitRolesResult(
                        PrimaryRole: unit.Template.PrimaryRole.ToString(),
                        SecondaryRole: unit.Template.SecondaryRole?.ToString()),
                    FinalState: new EvalUnitFinalStateResult(
                        Alive: unit.IsAlive,
                        Hp: unit.Resources.HP,
                        Mana: unit.Resources.Mana),
                    Performance: new EvalUnitPerformanceResult(
                        DamageDealt: totals.DamageDealt,
                        DamageTaken: totals.DamageTaken,
                        HealingDone: totals.HealingDone,
                        Kills: totals.Kills,
                        Deaths: totals.Deaths,
                        BuffEffectsApplied: totals.BuffEffectsApplied,
                        DebuffEffectsApplied: totals.DebuffEffectsApplied,
                        BuffUptimeTicksGranted: totals.BuffUptimeTicksGranted,
                        DebuffUptimeTicksGranted: totals.DebuffUptimeTicksGranted));
            })
            .ToArray();
    }

    private static IReadOnlyList<EvalTeamResult> BuildTeamResults(
        IReadOnlyList<EvalUnitResult> units,
        IReadOnlyList<EvalActionRecord> actions)
    {
        return units
            .GroupBy(unit => new { unit.TeamId, unit.Side })
            .OrderBy(group => group.Key.TeamId)
            .Select(group =>
            {
                var teamUnits = group.ToArray();
                var abilityCasts = actions.Count(action =>
                    action.ActingTeam == group.Key.TeamId &&
                    string.Equals(action.ActionType, "UseAbilityAction", StringComparison.Ordinal));

                var rolesPresent = teamUnits
                    .SelectMany(unit => new[] { unit.Roles.PrimaryRole, unit.Roles.SecondaryRole })
                    .OfType<string>()
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(role => role, StringComparer.Ordinal)
                    .ToArray();

                var unitTemplateIds = teamUnits
                    .Select(unit => unit.UnitTemplateId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray();

                return new EvalTeamResult(
                    TeamId: group.Key.TeamId,
                    Side: group.Key.Side,
                    UnitTemplateIds: unitTemplateIds,
                    RolesPresent: rolesPresent,
                    FinalState: new EvalTeamFinalStateResult(
                        AliveCount: teamUnits.Count(unit => unit.FinalState.Alive),
                        TotalHp: teamUnits.Sum(unit => unit.FinalState.Hp),
                        TotalMana: teamUnits.Sum(unit => unit.FinalState.Mana)),
                    Performance: new EvalTeamPerformanceResult(
                        DamageDealt: teamUnits.Sum(unit => unit.Performance.DamageDealt),
                        DamageTaken: teamUnits.Sum(unit => unit.Performance.DamageTaken),
                        HealingDone: teamUnits.Sum(unit => unit.Performance.HealingDone),
                        Kills: teamUnits.Sum(unit => unit.Performance.Kills),
                        Deaths: teamUnits.Sum(unit => unit.Performance.Deaths),
                        AbilityCasts: abilityCasts));
            })
            .ToArray();
    }

    private static EvalMatchResult BuildMatchResult(
        EngineFacade engine,
        IGameStateSpec gameStateSpec,
        IReadOnlyList<EvalActionRecord> actions,
        int seed,
        int maxTurns,
        string scenarioId)
    {
        var state = engine.GetState();
        var outcome = engine.GetOutcome();

        return new EvalMatchResult(
            ScenarioId: scenarioId,
            Seed: seed,
            AttackerTeamId: gameStateSpec.AttackerTeamId.Value,
            DefenderTeamId: gameStateSpec.DefenderTeamId.Value,
            Outcome: MapOutcome(outcome, gameStateSpec),
            TerminationReason: MapTerminationReason(outcome, state.Turn.AttackerTurnsTaken, gameStateSpec, maxTurns),
            AttackerTurnsTaken: state.Turn.AttackerTurnsTaken,
            TurnsPlayed: CountTurnsPlayed(actions),
            ActionCount: actions.Count,
            Map: BuildMatchMapResult(state, gameStateSpec.MapSpec));
    }

    private static string MapOutcome(Core.Game.Match.GameOutcome outcome, IGameStateSpec gameStateSpec)
    {
        if (outcome.Type == Core.Game.Match.GameOutcomeType.Draw)
            return "draw";

        if (outcome.Type != Core.Game.Match.GameOutcomeType.Victory || outcome.WinningTeam is null)
            return "invalid";

        if (outcome.WinningTeam == gameStateSpec.AttackerTeamId)
            return "attacker";

        if (outcome.WinningTeam == gameStateSpec.DefenderTeamId)
            return "defender";

        return "invalid";
    }

    private static string MapTerminationReason(
        Core.Game.Match.GameOutcome outcome,
        int attackerTurnsTaken,
        IGameStateSpec gameStateSpec,
        int maxTurns)
    {
        if (outcome.Type == Core.Game.Match.GameOutcomeType.Draw)
            return "draw";

        if (outcome.Type != Core.Game.Match.GameOutcomeType.Victory || outcome.WinningTeam is null)
            return "invalid";

        if (outcome.WinningTeam == gameStateSpec.DefenderTeamId && attackerTurnsTaken > maxTurns)
            return "turn_limit";

        return "last_team_standing";
    }

    private static int CountTurnsPlayed(IReadOnlyList<EvalActionRecord> actions)
    {
        return actions
            .Select(action => (action.TurnIndex, action.ActingTeam))
            .Distinct()
            .Count();
    }

    private static EvalMatchMapResult BuildMatchMapResult(
        Core.Game.State.ReadOnly.IReadOnlyGameState state,
        IMapSpec mapSpec)
    {
        var tileDistributionSpec = mapSpec.TileDistribution
            .OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal)
            .ToDictionary(
                entry => ToTelemetryKey(entry.Key),
                entry => entry.Value,
                StringComparer.Ordinal);

        var tileCountsActual = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var col = 0; col < state.Map.Width; col++)
        {
            for (var row = 0; row < state.Map.Height; row++)
            {
                var coord = HexCoordConverter.FromOffset(col, row);
                if (!state.Map.TryGetTile(coord, out var tile))
                    continue;

                var key = ToTelemetryKey(tile.Terrain);
                tileCountsActual[key] = tileCountsActual.TryGetValue(key, out var count) ? count + 1 : 1;
            }
        }

        return new EvalMatchMapResult(
            Width: state.Map.Width,
            Height: state.Map.Height,
            TileDistributionSpec: tileDistributionSpec,
            TileCountsActual: tileCountsActual.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal));
    }

    private static string ToTelemetryKey(TerrainType terrain)
        => terrain.ToString().ToLowerInvariant();
}
