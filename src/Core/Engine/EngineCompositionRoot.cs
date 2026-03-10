namespace Core.Engine;

using Core.Domain.Repositories;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Execution;
using Core.Engine.Effects;
using Core.Engine.Effects.Components.Calculators;
using Core.Engine.Effects.Components.Factories;
using Core.Engine.Effects.Components.Factories.Creators;
using Core.Engine.Effects.Components.Factories.Registry;
using Core.Engine.Effects.Factories;
using Core.Engine.Random;
using Core.Engine.Rules;
using Core.Engine.Victory;
using Core.Game.Session;
using Core.Map.Pathfinding;

public static class EngineCompositionRoot
{
    public static EngineFacade Create(GameSession session, int turnCount)
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
            gameOverEvaluator);
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
        var effectComponentIdFactory = new EffectComponentInstanceIdFactory();

        var componentCreators = BuildEffectComponentCreatorRegistry(effectComponentIdFactory);
        var effectComponentFactory = new EffectComponentInstanceFactory(componentCreators);

        var effectIdFactory = new EffectInstanceIdFactory();
        var effectFactory = new EffectInstanceFactory(
            effectIdFactory,
            effectComponentFactory,
            session.Context.Content);

        var derivedStatsCalculator = BuildDerivedStatsCalculator();
        var damageCalculator = BuildDamageCalculator();
        var healCalculator = BuildHealCalculator();

        return new EffectManager(
            effectFactory,
            derivedStatsCalculator,
            damageCalculator,
            healCalculator);
    }

    private static ComponentInstanceCreatorRegistry BuildEffectComponentCreatorRegistry(
        EffectComponentInstanceIdFactory idFactory)
    {
        return new ComponentInstanceCreatorRegistry(
        [
            new DamageOverTimeCreator(idFactory),
            new FlatAttributeModifierCreator(idFactory),
            new HealOverTimeCreator(idFactory),
            new InstantDamageCreator(idFactory),
            new InstantHealCreator(idFactory),
            new PercentAttributeModifierCreator(idFactory),
        ]);
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
        dispatcher.Register<ChangeActiveUnitAction>(BuildChangeActiveUnitHandler());
        dispatcher.Register<MoveAction>(BuildMoveHandler(pathfinder));
        dispatcher.Register<SkipActiveUnitAction>(BuildSkipActiveUnitHandler());
        dispatcher.Register<UseAbilityAction>(BuildUseAbilityHandler(abilities, pathfinder, effectManager));
    }

    private static ChangeActiveUnitActionHandler BuildChangeActiveUnitHandler()
        => new();

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
