namespace Setup.Build.GameState.Unit;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Game.Bootstrap.Contracts;
using Core.Map.Search;
using Setup.Config;
using Setup.Validation;
using Setup.Validation.Primitives;

public sealed class UnitSpawnSpecBuilder : IUnitSpawnSpecBuilder
{
    public IReadOnlyList<IUnitSpawnSpec> Build(
        GameStateConfig gameStateConfig,
        string configPath,
        IMapSpec mapSpec,
        TemplateRegistry templateRegistry,
        ValidationCollector issues)
    {
        ArgumentNullException.ThrowIfNull(gameStateConfig);
        ArgumentNullException.ThrowIfNull(configPath);
        ArgumentNullException.ThrowIfNull(mapSpec);
        ArgumentNullException.ThrowIfNull(templateRegistry);
        ArgumentNullException.ThrowIfNull(issues);

        var unitSpawns = new List<IUnitSpawnSpec>(gameStateConfig.Units.Count);

        for (var i = 0; i < gameStateConfig.Units.Count; i++)
        {
            var spawnPath = ContentSchema.GameStateUnit(configPath, i);
            var spawn = gameStateConfig.Units[i];

            if (!TryValidateSpawn(
                    spawn,
                    spawnPath,
                    mapSpec,
                    templateRegistry,
                    issues,
                    out var templateId,
                    out var teamId,
                    out var position))
            {
                continue;
            }

            unitSpawns.Add(new UnitSpawnSpec(
                unitTemplateId: templateId,
                teamId: teamId,
                position: position));
        }

        return unitSpawns;
    }

    private static bool TryValidateSpawn(
        GameStateUnitSpawnConfig spawn,
        string spawnPath,
        IMapSpec mapSpec,
        TemplateRegistry templateRegistry,
        ValidationCollector issues,
        out UnitTemplateId templateId,
        out TeamId teamId,
        out HexCoord position)
    {
        templateId = default;
        teamId = default;
        position = default;
        var isValid = true;

        if (spawn.TeamId <= 0)
        {
            issues.Add(ContentIssueFactory.InvalidTeam(
                ContentSchema.Property(spawnPath, ContentSchema.Fields.TeamId),
                spawn.TeamId));
            isValid = false;
        }
        else
        {
            teamId = new TeamId(spawn.TeamId);
        }

        templateId = new UnitTemplateId(spawn.Id);
        if (!templateRegistry.Units.TryGet(templateId, out _))
        {
            issues.Add(ContentIssueFactory.UnknownReference(
                ContentSchema.Property(spawnPath, ContentSchema.Fields.Id),
                "unit template",
                spawn.Id));
            isValid = false;
        }

        position = new HexCoord(spawn.Q, spawn.R);
        var (col, row) = HexCoordConverter.ToOffset(position);
        if (!IsInside(mapSpec, col, row))
        {
            issues.Add(ContentIssueFactory.SpawnOutOfBounds(
                spawnPath,
                spawn.Q,
                spawn.R,
                col,
                row,
                mapSpec.Width,
                mapSpec.Height));
            isValid = false;
        }

        return isValid;
    }

    private static bool IsInside(IMapSpec mapSpec, int col, int row)
        => col >= 0 && col < mapSpec.Width &&
           row >= 0 && row < mapSpec.Height;
}
