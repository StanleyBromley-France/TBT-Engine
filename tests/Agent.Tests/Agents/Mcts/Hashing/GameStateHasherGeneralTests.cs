using Agent.Tests.Agents.Mcts.Hashing.TestSupport;
using Agent.Tests.Engine.TestSupport;
using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Templates;
using Core.Engine;
using Core.Engine.Actions.Choice;
using Core.Game.Match;
using Core.Game.Session;
using Core.Map.Grid;

namespace Agent.Tests.Agents.Mcts.Hashing;

public sealed class GameStateHasherGeneralTests
{
    [Fact]
    public void Compute_EqualClone_ReturnsSameKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var state = GameStateHasherTestSupport.CreateBaselineState();
        var clone = state.DeepCloneForSimulation();

        var originalKey = hasher.Compute(state);
        var cloneKey = hasher.Compute(clone);

        Assert.Equal(originalKey, cloneKey);
    }

    [Fact]
    public void Compute_DifferentDictionaryInsertionOrder_ReturnsSameKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();
        var attacker = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(0, 0));
        var defender = EngineTestFactory.CreateUnit(2, team: 2, position: new HexCoord(1, 0));

        var left = EngineTestFactory.CreateState(
            new[] { attacker, defender },
            teamToAct: 1);

        var right = EngineTestFactory.CreateState(
            new[] { defender.DeepCloneForSimulation(), attacker.DeepCloneForSimulation() },
            teamToAct: 1);

        var leftKey = hasher.Compute(left);
        var rightKey = hasher.Compute(right);

        Assert.Equal(leftKey, rightKey);
    }

    [Fact]
    public void Compute_EquivalentActionSequences_WithDifferentActionOrder_ReturnSameKey()
    {
        var hasher = GameStateHasherTestSupport.CreateHasher();

        var leftEngine = CreateCommutingActionEngine();
        var rightEngine = CreateCommutingActionEngine();

        ApplyActions(
            leftEngine,
            new ActionChoice[]
            {
                new UseAbilityAction(new UnitInstanceId(2), new AbilityId("fortify"), new UnitInstanceId(1)),
                new MoveAction(new UnitInstanceId(2), new HexCoord(1, 1)),
                new MoveAction(new UnitInstanceId(1), new HexCoord(1, 0)),
                new UseAbilityAction(new UnitInstanceId(1), new AbilityId("shoot"), new UnitInstanceId(3)),
            });

        ApplyActions(
            rightEngine,
            new ActionChoice[]
            {
                new MoveAction(new UnitInstanceId(1), new HexCoord(1, 0)),
                new UseAbilityAction(new UnitInstanceId(1), new AbilityId("shoot"), new UnitInstanceId(3)),
                new UseAbilityAction(new UnitInstanceId(2), new AbilityId("fortify"), new UnitInstanceId(1)),
                new MoveAction(new UnitInstanceId(2), new HexCoord(1, 1)),
            });

        var leftKey = hasher.Compute(leftEngine.GetState());
        var rightKey = hasher.Compute(rightEngine.GetState());

        Assert.Equal(leftKey, rightKey);
    }

    private static EngineFacade CreateCommutingActionEngine()
    {
        var strikeAbility = EngineTestFactory.CreateAbility(
            id: "shoot",
            manaCost: 0,
            targetType: TargetType.Enemy,
            range: 10,
            effectId: "shoot-effect");

        var fortifyAbility = EngineTestFactory.CreateAbility(
            id: "fortify",
            manaCost: 0,
            targetType: TargetType.Ally,
            range: 10,
            effectId: "fortify-effect");

        var allyOne = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(0, 0), abilityIds: strikeAbility.Id);
        var allyTwo = EngineTestFactory.CreateUnit(2, team: 1, position: new HexCoord(0, 1), abilityIds: fortifyAbility.Id);
        var enemy = EngineTestFactory.CreateUnit(3, team: 2, position: new HexCoord(2, 0));

        var state = EngineTestFactory.CreateState(
            new[] { allyOne, allyTwo, enemy },
            teamToAct: 1,
            attackerTurnsTaken: 2);

        var abilityRepository = new AbilityRepository(
            new[]
            {
                new KeyValuePair<AbilityId, Ability>(strikeAbility.Id, strikeAbility),
                new KeyValuePair<AbilityId, Ability>(fortifyAbility.Id, fortifyAbility)
            });

        var effects = new EffectTemplateRepository(
            new Dictionary<EffectTemplateId, EffectTemplate>
            {
                [new EffectTemplateId("shoot-effect")] = new TestEffectTemplate(
                    new EffectTemplateId("shoot-effect"),
                    totalTicks: 1,
                    new EffectComponentTemplateId("shoot-damage")),
                [new EffectTemplateId("fortify-effect")] = new TestEffectTemplate(
                    new EffectTemplateId("fortify-effect"),
                    totalTicks: 1,
                    new EffectComponentTemplateId("fortify-max-hp"))
            });

        var effectComponents = new EffectComponentTemplateRepository(
            new Dictionary<EffectComponentTemplateId, EffectComponentTemplate>
            {
                [new EffectComponentTemplateId("shoot-damage")] = new InstantDamageComponentTemplate(
                    new EffectComponentTemplateId("shoot-damage"),
                    damage: 3,
                    damageType: DamageType.Physical,
                    critChance: 0,
                    critMultiplier: 1f),
                [new EffectComponentTemplateId("fortify-max-hp")] = new FlatAttributeModifierComponentTemplate(
                    new EffectComponentTemplateId("fortify-max-hp"),
                    stat: Core.Domain.Effects.Stats.StatType.MaxHP,
                    amount: 2)
            });

        var registry = new TemplateRegistry(
            units: new UnitTemplateRepository(new Dictionary<UnitTemplateId, UnitTemplate>()),
            abilities: abilityRepository,
            effects: effects,
            effectComponents: effectComponents);

        var session = new GameSession(
            new GameContext(
                content: registry,
                teams: new TeamPair(new TeamId(1), new TeamId(2))),
            new GameRuntime(
                state,
                new Core.Undo.UndoHistory(),
                GameOutcome.Ongoing()));

        return EngineCompositionRoot.Create(session, turnCount: 12);
    }

    private static void ApplyActions(EngineFacade engine, IEnumerable<ActionChoice> actions)
    {
        foreach (var action in actions)
        {
            var legalActions = engine.GetLegalActions().ToList();
            if (!legalActions.Any(legal => AreSameAction(legal, action)))
            {
                var description = string.Join(", ", legalActions.Select(DescribeAction));
                throw new InvalidOperationException(
                    $"Action '{DescribeAction(action)}' was not legal. Legal actions: {description}");
            }

            engine.ApplyAction(action);
        }
    }

    private static bool AreSameAction(ActionChoice left, ActionChoice right)
    {
        if (left.GetType() != right.GetType())
            return false;

        return (left, right) switch
        {
            (MoveAction l, MoveAction r) => l.UnitId == r.UnitId && l.TargetHex == r.TargetHex,
            (SkipActiveUnitAction l, SkipActiveUnitAction r) => l.UnitId == r.UnitId,
            (UseAbilityAction l, UseAbilityAction r) => l.UnitId == r.UnitId && l.AbilityId == r.AbilityId && l.Target == r.Target,
            _ => false
        };
    }

    private static string DescribeAction(ActionChoice action)
    {
        return action switch
        {
            MoveAction move => $"Move({move.UnitId}->{move.TargetHex})",
            SkipActiveUnitAction skip => $"Skip({skip.UnitId})",
            UseAbilityAction use => $"UseAbility({use.UnitId},{use.AbilityId},{use.Target})",
            _ => action.GetType().Name
        };
    }

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(
            EffectTemplateId id,
            int totalTicks,
            params EffectComponentTemplateId[] componentIds)
            : base(
                id,
                name: id.Value,
                isHarmful: false,
                totalTicks: totalTicks,
                maxStacks: 1,
                components: componentIds)
        {
        }
    }
}

