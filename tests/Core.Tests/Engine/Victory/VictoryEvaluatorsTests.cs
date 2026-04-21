using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Abilities;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Units;
using Core.Domain.Units.Templates;
using Core.Undo;
using Core.Engine.Victory;
using Core.Game.Match;
using Core.Map.Grid;
using Core.Game.Session;
using Core.Game.State;
using Core.Tests.Engine.TestSupport;

namespace Core.Tests.Engine.Victory;

public class VictoryEvaluatorsTests
{
    [Fact]
    public void LastTeamStanding_Returns_Draw_When_No_Alive_Units()
    {
        var session = CreateSession(Array.Empty<UnitInstance>());
        var evaluator = new LastTeamStandingEvaluator();

        var result = evaluator.Evaluate(session);

        Assert.Equal(GameOutcomeType.Draw, result.Type);
        Assert.Null(result.WinningTeam);
    }

    [Fact]
    public void LastTeamStanding_Returns_Victory_For_Only_Alive_Team()
    {
        var session = CreateSession(new[]
        {
            CreateUnit(id: 1, team: 1, hp: 10),
            CreateUnit(id: 2, team: 2, hp: 0)
        });
        var evaluator = new LastTeamStandingEvaluator();

        var result = evaluator.Evaluate(session);

        Assert.Equal(GameOutcomeType.Victory, result.Type);
        Assert.Equal(new TeamId(1), result.WinningTeam);
    }

    [Fact]
    public void LastTeamStanding_Returns_Ongoing_When_Multiple_Teams_Alive()
    {
        var session = CreateSession(new[]
        {
            CreateUnit(id: 1, team: 1, hp: 10),
            CreateUnit(id: 2, team: 2, hp: 10)
        });
        var evaluator = new LastTeamStandingEvaluator();

        var result = evaluator.Evaluate(session);

        Assert.Equal(GameOutcomeType.Ongoing, result.Type);
    }

    [Fact]
    public void TurnLimitDefenderWins_Throws_When_MaxTurns_Is_Not_Positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurnLimitDefenderWinsEvaluator(0));
    }

    [Fact]
    public void TurnLimitDefenderWins_Returns_Ongoing_At_Limit()
    {
        var session = CreateSession(
            units: new[] { CreateUnit(id: 1, team: 1, hp: 10) },
            attackerTurnsTaken: 3,
            teamToAct: 1);
        var evaluator = new TurnLimitDefenderWinsEvaluator(maxTurns: 3);

        var result = evaluator.Evaluate(session);

        Assert.Equal(GameOutcomeType.Ongoing, result.Type);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void TurnLimitDefenderWins_Returns_Defender_After_Limit(int teamToAct)
    {
        var session = CreateSession(
            units: new[] { CreateUnit(id: 1, team: 1, hp: 10) },
            attackerTurnsTaken: 4,
            teamToAct: teamToAct);
        var evaluator = new TurnLimitDefenderWinsEvaluator(maxTurns: 3);

        var result = evaluator.Evaluate(session);

        Assert.Equal(GameOutcomeType.Victory, result.Type);
        Assert.Equal(new TeamId(2), result.WinningTeam);
    }

    [Fact]
    public void CompositeEvaluator_Returns_First_Non_Ongoing_And_Skips_Null_Entries()
    {
        var session = CreateSession(new[]
        {
            CreateUnit(id: 1, team: 1, hp: 10)
        });
        var first = new FixedEvaluator(GameOutcome.Ongoing());
        var winner = GameOutcome.Victory(new TeamId(2));
        var second = new FixedEvaluator(winner);
        var third = new FixedEvaluator(GameOutcome.Draw());
        var evaluator = new CompositeGameOverEvaluator(new IGameOverEvaluator[]
        {
            first, null!, second, third
        });

        var result = evaluator.Evaluate(session);

        Assert.Equal(GameOutcomeType.Victory, result.Type);
        Assert.Equal(new TeamId(2), result.WinningTeam);
        Assert.Equal(1, first.CallCount);
        Assert.Equal(1, second.CallCount);
        Assert.Equal(0, third.CallCount);
    }

    [Fact]
    public void CompositeEvaluator_Returns_Ongoing_When_All_Are_Ongoing()
    {
        var session = CreateSession(Array.Empty<UnitInstance>());
        var evaluator = new CompositeGameOverEvaluator(new IGameOverEvaluator[]
        {
            new FixedEvaluator(GameOutcome.Ongoing()),
            new FixedEvaluator(GameOutcome.Ongoing())
        });

        var result = evaluator.Evaluate(session);

        Assert.Equal(GameOutcomeType.Ongoing, result.Type);
    }

    private sealed class FixedEvaluator : IGameOverEvaluator
    {
        private readonly GameOutcome _result;
        public int CallCount { get; private set; }

        public FixedEvaluator(GameOutcome result)
        {
            _result = result;
        }

        public GameOutcome Evaluate(GameSession session)
        {
            CallCount++;
            return _result;
        }
    }

    private static UnitInstance CreateUnit(int id, int team, int hp)
    {
        var template = new UnitTemplate(
            new UnitTemplateId($"unit-{id}"),
            $"Unit {id}",
            RoleType.Damage,
            null,
            new UnitBaseStats(maxHp: 10, maxManaPoints: 5, movePoints: 3, physicalDamageModifier: 100, magicDamageModifier: 100),
            Array.Empty<AbilityId>());
        var unit = new UnitInstance(new UnitInstanceId(id), new TeamId(team), template, new HexCoord(id, 0));
        unit.Resources.HP = hp;
        return unit;
    }

    private static GameSession CreateSession(IEnumerable<UnitInstance> units, int attackerTurnsTaken = 0, int teamToAct = 1)
    {
        var unitList = units.ToList();
        var state = new GameState(
            map: CreateMap(8, 8),
            unitInstances: unitList.ToDictionary(u => u.Id, u => u),
            activeEffects: unitList.ToDictionary(
                u => u.Id,
                _ => new Dictionary<EffectInstanceId, EffectInstance>()),
            turn: new Turn(attackerTurnsTaken, new TeamId(teamToAct)),
            phase: new ActivationPhase(),
            rng: new RngState(seed: 123, position: 0));

        var registry = new TemplateRegistry(
            units: new EmptyUnitTemplateRepository(),
            abilities: new AbilityRepository(new Dictionary<AbilityId, Ability>()),
            effects: new EffectTemplateRepository(new Dictionary<EffectTemplateId, EffectTemplate>()),
            effectComponents: new EffectComponentTemplateRepository(new Dictionary<EffectComponentTemplateId, EffectComponentTemplate>()));

        var context = new GameContext(
            content: registry,
            teams: new TeamPair(new TeamId(1), new TeamId(2)),
            sessionServices: EngineTestFactory.CreateSessionServices(registry));

        var runtime = new GameRuntime(
            state: state,
            undo: new UndoHistory(),
            outcome: GameOutcome.Ongoing(),
            instanceAllocation: new InstanceAllocationState());


        return new GameSession(
            context: context,
            runtime: runtime);
    }

    private static Core.Map.Grid.Map CreateMap(int width, int height)
    {
        var tiles = new Tile[width, height];

        for (var col = 0; col < width; col++)
            for (var row = 0; row < height; row++)
                tiles[col, row] = new Tile();

        return new Core.Map.Grid.Map(tiles);
    }

    private sealed class EmptyUnitTemplateRepository : IUnitTemplateRepository
    {
        public UnitTemplate Get(UnitTemplateId id) => throw new KeyNotFoundException($"Unknown unit template id '{id}'.");

        public bool TryGet(UnitTemplateId id, out UnitTemplate template)
        {
            template = null!;
            return false;
        }

        public IReadOnlyDictionary<UnitTemplateId, UnitTemplate> GetAll() =>
            new Dictionary<UnitTemplateId, UnitTemplate>();
    }
}
