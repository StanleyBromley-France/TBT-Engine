namespace Setup.Build.GameState;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Game.Bootstrap.Contracts;
using Setup.Build.GameState.Map;
using Setup.Build.GameState.Results;
using Setup.Build.GameState.Unit;
using Setup.Config;
using Setup.Loading;
using Setup.Validation;
using Setup.Validation.Primitives;

public sealed class GameStateSpecBuilder : IGameStateSpecBuilder
{
    private readonly IMapSpecBuilder _mapSpecBuilder;
    private readonly IUnitSpawnSpecBuilder _unitSpawnSpecBuilder;

    public GameStateSpecBuilder(
        IMapSpecBuilder mapSpecBuilder,
        IUnitSpawnSpecBuilder unitSpawnSpecBuilder)
    {
        _mapSpecBuilder = mapSpecBuilder ?? throw new ArgumentNullException(nameof(mapSpecBuilder));
        _unitSpawnSpecBuilder = unitSpawnSpecBuilder ?? throw new ArgumentNullException(nameof(unitSpawnSpecBuilder));
    }

    public GameStateSpecBuildResult Build(
        ContentPack pack,
        TemplateRegistry templateRegistry,
        string gameStateId,
        ContentValidationMode mode)
    {
        _ = mode;
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(templateRegistry);
        ArgumentNullException.ThrowIfNull(gameStateId);

        var issues = new ValidationCollector();

        if (!TryResolveGameState(pack, gameStateId, issues, out var gameStateConfig, out var configPath))
        {
            return new GameStateSpecBuildResult(null, issues);
        }

        ValidateTeams(gameStateConfig, configPath, issues);

        var mapSpecResult = _mapSpecBuilder.Build(
            gameStateConfig.MapGen,
            ContentSchema.Property(configPath, ContentSchema.Fields.MapGen));

        foreach (var mapIssue in mapSpecResult.IssueView.Issues)
        {
            issues.Add(mapIssue);
        }

        IReadOnlyList<IUnitSpawnSpec> unitSpawns = Array.Empty<IUnitSpawnSpec>();
        if (mapSpecResult.MapSpec is not null)
        {
            unitSpawns = _unitSpawnSpecBuilder.Build(
                gameStateConfig,
                configPath,
                mapSpecResult.MapSpec,
                templateRegistry,
                issues);
        }

        if (issues.HasErrors || mapSpecResult.MapSpec is null)
        {
            return new GameStateSpecBuildResult(null, issues);
        }

        var spec = new GameStateSpec(
            id: gameStateConfig.Id,
            mapSpec: mapSpecResult.MapSpec,
            attackerTeamId: new TeamId(gameStateConfig.AttackerTeamId),
            defenderTeamId: new TeamId(gameStateConfig.DefenderTeamId),
            initialTurn: new Turn(gameStateConfig.AttackerTurnsTaken, new TeamId(gameStateConfig.TeamToAct)),
            unitSpawns: unitSpawns);

        return new GameStateSpecBuildResult(spec, issues);
    }

    private static bool TryResolveGameState(
        ContentPack pack,
        string gameStateId,
        ValidationCollector issues,
        out GameStateConfig gameStateConfig,
        out string configPath)
    {
        gameStateConfig = null!;
        configPath = string.Empty;

        var matchIndices = new List<int>();
        for (var i = 0; i < pack.GameStates.Count; i++)
        {
            var candidate = pack.GameStates[i];
            if (string.Equals(candidate.Id, gameStateId, StringComparison.Ordinal))
            {
                matchIndices.Add(i);
            }
        }

        if (matchIndices.Count == 0)
        {
            issues.Add(ContentIssueFactory.UnknownReference(ContentSchema.Fields.GameStateId, "game state", gameStateId));
            return false;
        }

        if (matchIndices.Count > 1)
        {
            foreach (var index in matchIndices)
            {
                issues.Add(ContentIssueFactory.DuplicateId(
                    ContentSchema.Property(ContentSchema.GameState(index), ContentSchema.Fields.Id),
                    gameStateId));
            }

            return false;
        }

        var matchedIndex = matchIndices[0];
        gameStateConfig = pack.GameStates[matchedIndex];
        configPath = ContentSchema.GameState(matchedIndex);
        return true;
    }

    private static void ValidateTeams(
        GameStateConfig gameStateConfig,
        string configPath,
        ValidationCollector issues)
    {
        if (gameStateConfig.AttackerTeamId <= 0)
        {
            issues.Add(ContentIssueFactory.InvalidTeam(
                ContentSchema.Property(configPath, ContentSchema.Fields.AttackerTeamId),
                gameStateConfig.AttackerTeamId));
        }

        if (gameStateConfig.DefenderTeamId <= 0)
        {
            issues.Add(ContentIssueFactory.InvalidTeam(
                ContentSchema.Property(configPath, ContentSchema.Fields.DefenderTeamId),
                gameStateConfig.DefenderTeamId));
        }

        if (gameStateConfig.TeamToAct != gameStateConfig.AttackerTeamId &&
            gameStateConfig.TeamToAct != gameStateConfig.DefenderTeamId)
        {
            issues.Add(ContentIssueFactory.InvalidTeamToAct(
                ContentSchema.Property(configPath, ContentSchema.Fields.TeamToAct),
                gameStateConfig.TeamToAct,
                gameStateConfig.AttackerTeamId,
                gameStateConfig.DefenderTeamId));
        }
    }
}
