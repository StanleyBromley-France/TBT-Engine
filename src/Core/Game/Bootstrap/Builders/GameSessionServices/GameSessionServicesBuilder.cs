namespace Core.Game.Bootstrap.Builders.GameSessionServices;

using Core.Domain.Repositories;
using Core.Game.Factories.EffectComponents;
using Core.Game.Factories.EffectComponents.Creators;
using Core.Game.Factories.EffectComponents.Registry;
using Core.Game.Factories.Effects;
using Core.Game.Factories.Units;
using Core.Game.Session;

internal sealed class GameSessionServicesBuilder : IGameSessionServicesBuilder
{
    public GameSessionServices Build(TemplateRegistry templateRegistry)
    {
        if (templateRegistry is null) throw new ArgumentNullException(nameof(templateRegistry));

        var unitInstaceIdFactory = new UnitInstanceIdFactory();
        var unitInstanceFactory = BuildUnitInstanceFactory(unitInstaceIdFactory, templateRegistry.Units);

        var effectComponentIdFactory = new EffectComponentInstanceIdFactory();
        var componentCreators = BuildEffectComponentCreatorRegistry(effectComponentIdFactory);
        var effectComponentFactory = new EffectComponentInstanceFactory(componentCreators);

        var effectIdFactory = new EffectInstanceIdFactory();
        var effectFactory = new EffectInstanceFactory(
            effectIdFactory,
            effectComponentFactory,
            templateRegistry);

        return new GameSessionServices(
            unitInstanceFactory,
            effectFactory);
    }

    private static IUnitInstanceFactory BuildUnitInstanceFactory(IUnitInstanceIdFactory unitInstanceIdFactory, IUnitTemplateRepository unitTemplateRepository)
        => new UnitInstanceFactory(unitInstanceIdFactory, unitTemplateRepository);

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
}
