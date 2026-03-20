namespace Setup.Build.TemplateRegistry;

using Core.Domain.Repositories;
using Setup.Build.TemplateRegistry.Builders;
using Setup.Build.TemplateRegistry.Builders.EffectComponents;
using Setup.Build.TemplateRegistry.Results;
using Setup.Loading;
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
        ContentPackTemplates packTemplates,
        ContentValidationMode mode)
    {
        var issues = new ValidationCollector();

        var builtComponents = EffectComponentRepoBuilder.Build(
            packTemplates.EffectComponents,
            issues,
            _effectComponentStrategies);
        var builtEffects = EffectRepoBuilder.Build(packTemplates.Effects, builtComponents, issues);
        var builtAbilities = AbilityRepoBuilder.Build(packTemplates.Abilities, builtEffects, issues);
        var builtUnits = UnitRepoBuilder.Build(packTemplates.Units, builtAbilities, issues);

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
