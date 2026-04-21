namespace Core.Engine;

using Core.Domain.Repositories;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Effects;
using Core.Engine.Effects.Components.Calculators;
using Core.Engine.Random;
using Core.Engine.Rules;
using Core.Engine.Telemetry;
using Core.Engine.Victory;
using Core.Game.Factories.EffectComponents;
using Core.Game.Factories.EffectComponents.Creators;
using Core.Game.Factories.EffectComponents.Registry;
using Core.Game.Factories.Effects;
using Core.Game.Session;
using Core.Map.Pathfinding;

public static class EngineCompositionRoot
{
    public static EngineFacade Create(
        GameSession session,
        int turnCount,
        ICombatTelemetrySink? combatTelemetry = null)
    {
        // Core
        var rng = BuildRng();
        var pathfinder = BuildPathfinder();

        // Content
        var abilities = session.Context.Content.Abilities;

        // Effects
        var effectManager = BuildEffectManager(session);

        // Rules
        var rules = BuildRules(pathfinder, abilities);

        // Actions
        var dispatcher = BuildDispatcher(pathfinder, abilities, effectManager);

        // Game-over
        var gameOverEvaluator = BuildGameOver(turnCount);

        return new EngineFacade(
            session,
            rules,
            dispatcher,
            rng,
            effectManager,
            gameOverEvaluator,
            combatTelemetry);
    }

    // -------------------------
    // Core
    // -------------------------

    private static DeterministicRng BuildRng()
        => new();

    private static Pathfinder BuildPathfinder()
        => new();


    // -------------------------
    // Effects
    // -------------------------

    private static IEffectManager BuildEffectManager(GameSession session)
    {
        var derivedStatsCalculator = BuildDerivedStatsCalculator();
        var damageCalculator = BuildDamageCalculator();
        var healCalculator = BuildHealCalculator();

        return new EffectManager(
            derivedStatsCalculator,
            damageCalculator,
            healCalculator);
    }

    private static DerivedStatsCalculator BuildDerivedStatsCalculator()
        => new();

    private static CritDamageCalculator BuildDamageCalculator()
        => new();

    private static HealCalculator BuildHealCalculator()
        => new();

    // -------------------------
    // Rules
    // -------------------------

    private static IActionRules BuildRules(Pathfinder pathfinder, IAbilityRepository abilities)
    {
        var validator = BuildActionValidator(pathfinder, abilities);
        var generator = BuildActionGenerator(pathfinder, abilities, validator);
        return new ActionRules(validator, generator);
    }

    private static ActionValidator BuildActionValidator(Pathfinder pathfinder, IAbilityRepository abilities)
        => new(pathfinder, abilities);

    private static ActionGenerator BuildActionGenerator(
        Pathfinder pathfinder,
        IAbilityRepository abilities,
        ActionValidator validator)
        => new(pathfinder, abilities, validator);

    // -------------------------
    // Actions
    // -------------------------

    private static ActionDispatcher BuildDispatcher(
        Pathfinder pathfinder,
        IAbilityRepository abilities,
        IEffectManager effectManager)
    {
        var dispatcher = new ActionDispatcher();

        RegisterActionHandlers(dispatcher, pathfinder, abilities, effectManager);

        return dispatcher;
    }

    private static void RegisterActionHandlers(
        ActionDispatcher dispatcher,
        Pathfinder pathfinder,
        IAbilityRepository abilities,
        IEffectManager effectManager)
    {
        dispatcher.Register<MoveAction>(BuildMoveHandler(pathfinder));
        dispatcher.Register<SkipActiveUnitAction>(BuildSkipActiveUnitHandler());
        dispatcher.Register<UseAbilityAction>(BuildUseAbilityHandler(abilities, pathfinder, effectManager));
    }

    private static MoveActionHandler BuildMoveHandler(Pathfinder pathfinder)
        => new(pathfinder);

    private static SkipActiveUnitHandler BuildSkipActiveUnitHandler()
        => new();

    private static UseAbilityActionHandler BuildUseAbilityHandler(
        IAbilityRepository abilities,
        Pathfinder pathfinder,
        IEffectManager effectManager)
        => new(abilities, pathfinder, effectManager);

    // -------------------------
    // Game-over
    // -------------------------

    private static IGameOverEvaluator BuildGameOver(int turnCount)
        => new CompositeGameOverEvaluator(
        [
            new LastTeamStandingEvaluator(),
            new TurnLimitDefenderWinsEvaluator(turnCount),
        ]);
}
