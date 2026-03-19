namespace Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders.Parsing;
using Setup.Config;
using Setup.Validation.Primitives;

internal sealed class DamageOverTimeComponentBuilder : IEffectComponentBuilder
{
    public bool TryBuild(
        EffectComponentTemplateConfig config,
        string path,
        EffectComponentTemplateId id,
        ValidationCollector issues,
        out EffectComponentTemplate built)
    {
        if (!EffectComponentContentParser.TryRequireInt(config.Damage, $"{path}.Damage", nameof(config.Damage), issues, out var damage) ||
            !EffectComponentContentParser.TryParseDamageType(config.DamageType, $"{path}.DamageType", issues, out var damageType))
        {
            built = null!;
            return false;
        }

        built = new DamageOverTimeComponentTemplate(id, damage, damageType);
        return true;
    }
}
