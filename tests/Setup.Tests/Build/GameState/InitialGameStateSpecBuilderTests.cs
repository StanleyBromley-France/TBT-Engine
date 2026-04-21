namespace Setup.Tests.Build.GameState;

using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Templates;
using Setup.Build.GameState;
using Setup.Build.GameState.Map;
using Setup.Build.GameState.Unit;
using Setup.Config;
using Setup.Loading;
using Setup.Validation.Primitives;

public sealed class InitialGameStateSpecBuilderTests
{
    [Fact]
    public void Build_Valid_Config_Creates_Initial_GameState_Spec()
    {
        var builder = CreateBuilder();
        var registry = CreateTemplateRegistry(["unit-1"]);
        var pack = CreatePack(
        [
            new GameStateConfig
            {
                Id = "scenario-1",
                AttackerTeamId = 1,
                DefenderTeamId = 2,
                TeamToAct = 1,
                AttackerTurnsTaken = 3,
                MapGen = new MapGenConfig
                {
                    Width = 4,
                    Height = 3,
                    TileDistribution = new Dictionary<string, double>
                    {
                        ["Plain"] = 1.0
                    }
                },
                Units =
                [
                    new GameStateUnitSpawnConfig
                    {
                        Id = "unit-1",
                        TeamId = 1,
                        Q = 1,
                        R = 1
                    },
                    new GameStateUnitSpawnConfig
                    {
                        Id = "unit-1",
                        TeamId = 2,
                        Q = 2,
                        R = 1
                    }
                ]
            }
        ]);

        var result = builder.Build(pack, registry, "scenario-1", ContentValidationMode.Strict);

        Assert.False(result.HasErrors);
        Assert.NotNull(result.Spec);

        var spec = result.Spec!;
        Assert.Equal("scenario-1", spec.Id);
        Assert.Equal(4, spec.MapSpec.Width);
        Assert.Equal(3, spec.MapSpec.Height);
        Assert.Equal(new TeamId(1), spec.AttackerTeamId);
        Assert.Equal(new TeamId(2), spec.DefenderTeamId);
        Assert.Equal(3, spec.InitialTurn.AttackerTurnsTaken);
        Assert.Equal(new TeamId(1), spec.InitialTurn.TeamToAct);
        Assert.Equal(2, spec.UnitSpawns.Count);

        var firstSpawn = spec.UnitSpawns[0];
        var secondSpawn = spec.UnitSpawns[1];
        Assert.Equal(new UnitTemplateId("unit-1"), firstSpawn.UnitTemplateId);
        Assert.Equal(new TeamId(1), firstSpawn.TeamId);
        Assert.Equal(new HexCoord(1, 1), firstSpawn.Position);
        Assert.Equal(new UnitTemplateId("unit-1"), secondSpawn.UnitTemplateId);
        Assert.Equal(new TeamId(2), secondSpawn.TeamId);
        Assert.Equal(new HexCoord(2, 1), secondSpawn.Position);
    }

    [Fact]
    public void Build_Unknown_GameStateId_Returns_Error()
    {
        var builder = CreateBuilder();
        var registry = CreateTemplateRegistry(["unit-1"]);
        var pack = CreatePack([CreateValidScenario("scenario-1")]);

        var result = builder.Build(pack, registry, "missing-id", ContentValidationMode.Strict);

        Assert.Null(result.Spec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i =>
            i.Code == ContentIssueFactory.UnknownReferenceCode &&
            i.Path == "GameStateId");
    }

    [Fact]
    public void Build_Duplicate_GameStateId_Returns_Error()
    {
        var builder = CreateBuilder();
        var registry = CreateTemplateRegistry(["unit-1"]);
        var pack = CreatePack(
        [
            CreateValidScenario("scenario-1"),
            CreateValidScenario("scenario-1")
        ]);

        var result = builder.Build(pack, registry, "scenario-1", ContentValidationMode.Strict);

        Assert.Null(result.Spec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i =>
            i.Code == ContentIssueFactory.DuplicateIdCode &&
            i.Path == "GameStates[0].Id");
        Assert.Contains(result.IssueView.Issues, i =>
            i.Code == ContentIssueFactory.DuplicateIdCode &&
            i.Path == "GameStates[1].Id");
    }

    [Fact]
    public void Build_Unknown_Unit_Template_Returns_Error()
    {
        var builder = CreateBuilder();
        var registry = CreateTemplateRegistry(["unit-1"]);
        var scenario = CreateValidScenario("scenario-1");
        scenario.Units[0].Id = "unknown-template";
        var pack = CreatePack([scenario]);

        var result = builder.Build(pack, registry, "scenario-1", ContentValidationMode.Strict);

        Assert.Null(result.Spec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i =>
            i.Code == ContentIssueFactory.UnknownReferenceCode &&
            i.Path == "GameStates[0].Units[0].Id");
    }

    [Fact]
    public void Build_Spawn_Out_Of_Bounds_Returns_Error()
    {
        var builder = CreateBuilder();
        var registry = CreateTemplateRegistry(["unit-1"]);
        var scenario = CreateValidScenario("scenario-1");
        scenario.Units[0].Q = 999;
        scenario.Units[0].R = 999;
        var pack = CreatePack([scenario]);

        var result = builder.Build(pack, registry, "scenario-1", ContentValidationMode.Strict);

        Assert.Null(result.Spec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i => i.Code == ContentIssueFactory.SpawnOutOfBoundsCode);
    }

    [Fact]
    public void Build_Invalid_TeamToAct_Returns_Error()
    {
        var builder = CreateBuilder();
        var registry = CreateTemplateRegistry(["unit-1"]);
        var scenario = CreateValidScenario("scenario-1");
        scenario.TeamToAct = 42;
        var pack = CreatePack([scenario]);

        var result = builder.Build(pack, registry, "scenario-1", ContentValidationMode.Strict);

        Assert.Null(result.Spec);
        Assert.True(result.HasErrors);
        Assert.Contains(result.IssueView.Issues, i =>
            i.Code == ContentIssueFactory.InvalidTeamToActCode &&
            i.Path == "GameStates[0].TeamToAct");
    }

    private static IGameStateSpecBuilder CreateBuilder()
        => new GameStateSpecBuilder(
            new MapSpecBuilder(),
            new UnitSpawnSpecBuilder());

    private static ContentPack CreatePack(IReadOnlyList<GameStateConfig> gameStates)
    {
        var pack = new ContentPack();
        var packBuilder = (IContentPackBuilder)pack;
        packBuilder.AddGameStates(gameStates.ToList());
        return pack;
    }

    private static GameStateConfig CreateValidScenario(string id)
    {
        return new GameStateConfig
        {
            Id = id,
            AttackerTeamId = 1,
            DefenderTeamId = 2,
            TeamToAct = 1,
            AttackerTurnsTaken = 0,
            MapGen = new MapGenConfig
            {
                Width = 3,
                Height = 3,
                TileDistribution = new Dictionary<string, double>
                {
                    ["Plain"] = 1.0
                }
            },
            Units =
            [
                new GameStateUnitSpawnConfig
                {
                    Id = "unit-1",
                    TeamId = 1,
                    Q = 1,
                    R = 1
                }
            ]
        };
    }

    private static TemplateRegistry CreateTemplateRegistry(IReadOnlyList<string> unitTemplateIds)
    {
        var units = unitTemplateIds.ToDictionary(
            id => new UnitTemplateId(id),
            id => new UnitTemplate(
                id: new UnitTemplateId(id),
                name: id,
                primaryRole: RoleType.Damage,
                secondaryRole: null,
                baseStats: new UnitBaseStats(
                    maxHp: 10,
                    maxManaPoints: 5,
                    movePoints: 3,
                    physicalDamageModifier: 100,
                    magicDamageModifier: 100),
                abilityIds: []));

        return new TemplateRegistry(
            units: new UnitTemplateRepository(units),
            abilities: new AbilityRepository(new Dictionary<AbilityId, Core.Domain.Abilities.Ability>()),
            effects: new EffectTemplateRepository(new Dictionary<EffectTemplateId, Core.Domain.Effects.Templates.EffectTemplate>()),
            effectComponents: new EffectComponentTemplateRepository(
                new Dictionary<EffectComponentTemplateId, Core.Domain.Effects.Components.Templates.EffectComponentTemplate>()));
    }
}
