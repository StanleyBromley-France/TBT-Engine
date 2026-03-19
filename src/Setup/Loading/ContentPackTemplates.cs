using Setup.Config;

namespace Setup.Loading;

public sealed class ContentPackTemplates : IContentPackTemplatesBuilder
{
    private readonly List<UnitTemplateConfig> _units = new List<UnitTemplateConfig>();
    private readonly List<AbilityConfig> _abilities = new List<AbilityConfig>();
    private readonly List<EffectTemplateConfig> _effects = new List<EffectTemplateConfig>();
    private readonly List<EffectComponentTemplateConfig> _effectComponents = new List<EffectComponentTemplateConfig>();

    public IReadOnlyList<UnitTemplateConfig> Units => _units;
    public IReadOnlyList<AbilityConfig> Abilities => _abilities;
    public IReadOnlyList<EffectTemplateConfig> Effects => _effects;
    public IReadOnlyList<EffectComponentTemplateConfig> EffectComponents => _effectComponents;

    void IContentPackTemplatesBuilder.AddUnits(List<UnitTemplateConfig> unitTemplates)
    {
        _units.AddRange(unitTemplates);
    }

    void IContentPackTemplatesBuilder.AddAbilities(List<AbilityConfig> abilities)
    {
        _abilities.AddRange(abilities);
    }

    void IContentPackTemplatesBuilder.AddEffects(List<EffectTemplateConfig> effectTemplates)
    {
        _effects.AddRange(effectTemplates);
    }

    void IContentPackTemplatesBuilder.AddEffectComponents(List<EffectComponentTemplateConfig> effectComponentTemplates)
    {
        _effectComponents.AddRange(effectComponentTemplates);
    }
}
