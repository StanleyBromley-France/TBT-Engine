using Setup.Config;

namespace Setup.Loading;

public interface IContentPackTemplatesBuilder
{
    void AddUnits(List<UnitTemplateConfig> unitTemplates);
    void AddAbilities(List<AbilityConfig> abilities);
    void AddEffects(List<EffectTemplateConfig> effectTemplates);
    void AddEffectComponents(List<EffectComponentTemplateConfig> effectComponentTemplates);
}
