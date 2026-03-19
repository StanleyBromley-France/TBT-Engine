namespace Setup.Build.TemplateRegistry;

using Setup.Build.TemplateRegistry.Results;
using Setup.Config;
using Setup.Validation.Primitives;

public interface ITemplateRegistryBuilder
{
    TemplateRegistryBuildResult Build(
        List<UnitTemplateConfig> unitConfigs,
        List<AbilityConfig> abilityConfigs,
        List<EffectTemplateConfig> effectConfigs,
        List<EffectComponentTemplateConfig> componentConfigs,
        ContentValidationMode mode);
}
