namespace Setup.Build.TemplateRegistry;

using Core.Domain.Repositories;
using Setup.Build.TemplateRegistry.Builders;
using Setup.Build.TemplateRegistry.Builders.EffectComponents;
using Setup.Build.TemplateRegistry.Results;
using Setup.Config;
using Setup.Validation.Primitives;

public sealed class TemplateRegistryBuilder : ITemplateRegistryBuilder
{
    private readonly IEffectComponentBuilderResolver _effectComponentStrategies;

    public IEffectComponentBuilderResolver EffectComponentStrategies => _effectComponentStrategies;

    public TemplateRegistryBuilder(IEffectComponentBuilderResolver effectComponentStrategies)
    {
        _effectComponentStrategies = effectComponentStrategies;
    }

    public TemplateRegistryBuildResult Build(
        List<UnitTemplateConfig> unitConfigs,
        List<AbilityConfig> abilityConfigs,
        List<EffectTemplateConfig> effectConfigs,
        List<EffectComponentTemplateConfig> componentConfigs,
        ContentValidationMode mode)
    {
        var issues = new ValidationCollector();

        var builtComponents = EffectComponentRepoBuilder.Build(
            componentConfigs,
            issues,
            _effectComponentStrategies);
        var builtEffects = EffectRepoBuilder.Build(effectConfigs, builtComponents, issues);
        var builtAbilities = AbilityRepoBuilder.Build(abilityConfigs, builtEffects, issues);
        var builtUnits = UnitRepoBuilder.Build(unitConfigs, builtAbilities, issues);

        if (mode == ContentValidationMode.Strict && issues.HasErrors)
        {
            return new TemplateRegistryBuildResult(null, issues);
        }

        var registry = new TemplateRegistry(
            units: new UnitTemplateRepository(builtUnits),
            abilities: new AbilityRepository(builtAbilities),
            effects: new EffectTemplateRepository(builtEffects),
            effectComponents: new EffectComponentTemplateRepository(builtComponents));

        return new TemplateRegistryBuildResult(registry, issues);
    }
}
