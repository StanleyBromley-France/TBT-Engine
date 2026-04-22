namespace Core.Tests.Game.Bootstrap;

using Core.Domain.Abilities;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Templates;
using Core.Game.Bootstrap;
using Core.Game.Bootstrap.Contracts;
using Core.Map.Terrain;

public sealed class GameSessionBootstrapperTests
{
    [Fact]
    public void Create_UsesRunSeedToInitializeSimulationRng()
    {
        var registry = CreateTemplateRegistry(["unit-1"]);
        var spec = new GameStateSpecStub(
            id: "scenario-1",
            mapSpec: new MapSpecStub(
                width: 4,
                height: 3,
                tileDistribution: new Dictionary<TerrainType, double>
                {
                    [TerrainType.Plain] = 1.0
                }),
            attackerTeamId: new TeamId(1),
            defenderTeamId: new TeamId(2),
            initialTurn: new Turn(0, new TeamId(1)),
            unitSpawns:
            [
                new UnitSpawnSpecStub(new UnitTemplateId("unit-1"), new TeamId(1), new HexCoord(0, 0))
            ]);

        var first = GameSessionBootstrapper.Create(registry, spec, 12345);
        var second = GameSessionBootstrapper.Create(registry, spec, 12345);
        var third = GameSessionBootstrapper.Create(registry, spec, 54321);

        Assert.Equal(first.Runtime.State.Rng.Seed, second.Runtime.State.Rng.Seed);
        Assert.Equal(0, first.Runtime.State.Rng.Position);
        Assert.NotEqual(first.Runtime.State.Rng.Seed, third.Runtime.State.Rng.Seed);
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
            abilities: new AbilityRepository(new Dictionary<AbilityId, Ability>()),
            effects: new EffectTemplateRepository(new Dictionary<EffectTemplateId, EffectTemplate>()),
            effectComponents: new EffectComponentTemplateRepository(
                new Dictionary<EffectComponentTemplateId, EffectComponentTemplate>()));
    }

    private sealed class GameStateSpecStub : IGameStateSpec
    {
        public string Id { get; }
        public IMapSpec MapSpec { get; }
        public TeamId AttackerTeamId { get; }
        public TeamId DefenderTeamId { get; }
        public Turn InitialTurn { get; }
        public IReadOnlyList<IUnitSpawnSpec> UnitSpawns { get; }

        public GameStateSpecStub(
            string id,
            IMapSpec mapSpec,
            TeamId attackerTeamId,
            TeamId defenderTeamId,
            Turn initialTurn,
            IReadOnlyList<IUnitSpawnSpec> unitSpawns)
        {
            Id = id;
            MapSpec = mapSpec;
            AttackerTeamId = attackerTeamId;
            DefenderTeamId = defenderTeamId;
            InitialTurn = initialTurn;
            UnitSpawns = unitSpawns;
        }
    }

    private sealed class MapSpecStub : IMapSpec
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyDictionary<TerrainType, double> TileDistribution { get; }

        public MapSpecStub(int width, int height, IReadOnlyDictionary<TerrainType, double> tileDistribution)
        {
            Width = width;
            Height = height;
            TileDistribution = tileDistribution;
        }
    }

    private sealed class UnitSpawnSpecStub : IUnitSpawnSpec
    {
        public UnitTemplateId UnitTemplateId { get; }
        public TeamId TeamId { get; }
        public HexCoord Position { get; }

        public UnitSpawnSpecStub(UnitTemplateId unitTemplateId, TeamId teamId, HexCoord position)
        {
            UnitTemplateId = unitTemplateId;
            TeamId = teamId;
            Position = position;
        }
    }
}
