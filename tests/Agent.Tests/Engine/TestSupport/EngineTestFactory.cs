using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;
using Core.Game.Match;
using Core.Game.Session;
using Core.Game.State;
using Core.Map.Grid;
using Core.Undo;

namespace Agent.Tests.Engine.TestSupport;

internal static class EngineTestFactory
{
    public static Ability CreateAbility(
        string id,
        int manaCost,
        TargetType targetType,
        int range = 1,
        bool requiresLos = false,
        int radius = 0,
        string? effectId = null)
    {
        return new Ability(
            new AbilityId(id),
            id,
            AbilityCategory.OffensiveSpell,
            manaCost,
            new TargetingRules(range, requiresLos, targetType, radius),
            new EffectTemplateId(effectId ?? $"{id}-effect"));
    }

    public static UnitInstance CreateUnit(
        int id,
        int team,
        HexCoord position,
        int hp = 10,
        int mana = 10,
        params AbilityId[] abilityIds)
    {
        var template = new UnitTemplate(
            new UnitTemplateId($"unit-{id}"),
            $"Unit {id}",
            new UnitBaseStats(maxHp: 10, maxManaPoints: 10, movePoints: 3, physicalDamageModifier: 100, magicDamageModifier: 100),
            abilityIds);

        var unit = new UnitInstance(
            new UnitInstanceId(id),
            new TeamId(team),
            template,
            position);

        unit.Resources.HP = hp;
        unit.Resources.Mana = mana;
        return unit;
    }

    public static GameState CreateState(
        IEnumerable<UnitInstance> units,
        int teamToAct,
        UnitInstanceId activeUnitId,
        int attackerTurnsTaken = 0)
    {
        var unitList = units.ToList();
        return new GameState(
            map: CreateMap(8, 8),
            unitInstances: unitList.ToDictionary(u => u.Id, u => u),
            activeEffects: unitList.ToDictionary(
                u => u.Id,
                _ => new Dictionary<EffectInstanceId, EffectInstance>()),
            turn: new Turn(attackerTurnsTaken, new TeamId(teamToAct)),
            phase: new ActivationPhase(activeUnitId),
            rng: new RngState(seed: 123, position: 0));
    }

    public static GameSession CreateSession(GameState state, IAbilityRepository abilities)
    {
        var registry = new TemplateRegistry(
            units: new UnitTemplateRepository(new Dictionary<UnitTemplateId, UnitTemplate>()),
            abilities: abilities,
            effects: new EffectTemplateRepository(new Dictionary<EffectTemplateId, EffectTemplate>()),
            effectComponents: new EffectComponentTemplateRepository(
                new Dictionary<EffectComponentTemplateId, EffectComponentTemplate>()));

        var context = new GameContext(
            content: registry,
            teams: new TeamPair(new TeamId(1), new TeamId(2)));

        var runtime = new GameRuntime(
            state: state,
            undo: new UndoHistory(),
            outcome: GameOutcome.Ongoing());

        return new GameSession(
            context: context,
            runtime: runtime);
    }

    private static Map CreateMap(int width, int height)
    {
        var tiles = new Tile[width, height];
        for (var col = 0; col < width; col++)
        {
            for (var row = 0; row < height; row++)
            {
                tiles[col, row] = new Tile();
            }
        }

        return new Map(tiles);
    }
}
